using System.Data;
using System.Reflection;
using AuthPermissions.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthPermissions.Bootstrap;

internal static class AuthPermissionsMigrationBootstrapper
{
    private static readonly string[] InitialSchemaTables =
    [
        "AspNetRoles",
        "AspNetUsers",
        "TenantDatabase",
        "AspNetRoleClaims",
        "AspNetUserClaims",
        "AspNetUserLogins",
        "AspNetUserRoles",
        "AspNetUserTokens",
        "Tenants"
    ];

    public static async Task BaselineExistingSchemaAsync(
        ApplicationDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(logger);

        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
            return;

        var allMigrations = ((IInfrastructure<IServiceProvider>)dbContext.Database)
            .Instance
            .GetRequiredService<IMigrationsAssembly>()
            .Migrations
            .Keys
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        if (allMigrations.Length == 0)
            return;

        var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        if (appliedMigrations.Length > 0)
            return;

        var initialMigrationId = allMigrations[0];
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        if (!pendingMigrations.Contains(initialMigrationId, StringComparer.Ordinal))
            return;

        var schemaState = await InspectInitialSchemaAsync(dbContext, cancellationToken);
        if (schemaState.ExistingTables.Length == 0)
            return;

        if (schemaState.MissingTables.Length > 0)
        {
            throw new InvalidOperationException(
                "Detected a partial existing AuthPermissions schema without EF migration history. " +
                $"Existing tables: {string.Join(", ", schemaState.ExistingTables)}. " +
                $"Missing tables: {string.Join(", ", schemaState.MissingTables)}. " +
                "Automatic baselining was skipped to avoid damaging existing data. " +
                "Align the database with the initial migration or recreate the auth database.");
        }

        await EnsureMigrationHistoryBaselineAsync(dbContext, initialMigrationId, cancellationToken);

        logger.LogWarning(
            "Detected an existing AuthPermissions schema without EF migration history. " +
            "Inserted baseline migration record {MigrationId} before running migrations.",
            initialMigrationId);
    }

    private static async Task<AuthSchemaState> InspectInitialSchemaAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            var parameterNames = new List<string>(InitialSchemaTables.Length);

            for (var i = 0; i < InitialSchemaTables.Length; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@p{i}";
                parameter.Value = InitialSchemaTables[i];
                command.Parameters.Add(parameter);
                parameterNames.Add(parameter.ParameterName);
            }

            command.CommandText = $"""
                SELECT t.name
                FROM sys.tables AS t
                INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
                WHERE s.name = N'dbo'
                  AND t.name IN ({string.Join(", ", parameterNames)})
                """;

            var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                existingTables.Add(reader.GetString(0));
            }

            var orderedExistingTables = existingTables
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var missingTables = InitialSchemaTables
                .Where(table => !existingTables.Contains(table))
                .ToArray();

            return new AuthSchemaState(orderedExistingTables, missingTables);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static async Task EnsureMigrationHistoryBaselineAsync(
        ApplicationDbContext dbContext,
        string migrationId,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                BEGIN TRY
                    IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[__EFMigrationsHistory] (
                            [MigrationId] nvarchar(150) NOT NULL,
                            [ProductVersion] nvarchar(32) NOT NULL,
                            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                        );
                    END
                END TRY
                BEGIN CATCH
                    IF ERROR_NUMBER() <> 2714
                        THROW;
                END CATCH;

                BEGIN TRY
                    IF NOT EXISTS (
                        SELECT 1
                        FROM [dbo].[__EFMigrationsHistory]
                        WHERE [MigrationId] = @migrationId
                    )
                    BEGIN
                        INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                        VALUES (@migrationId, @productVersion);
                    END
                END TRY
                BEGIN CATCH
                    IF ERROR_NUMBER() NOT IN (2601, 2627)
                        THROW;
                END CATCH;
                """;

            var migrationIdParameter = command.CreateParameter();
            migrationIdParameter.ParameterName = "@migrationId";
            migrationIdParameter.Value = migrationId;
            command.Parameters.Add(migrationIdParameter);

            var productVersionParameter = command.CreateParameter();
            productVersionParameter.ParameterName = "@productVersion";
            productVersionParameter.Value = GetEntityFrameworkProductVersion();
            command.Parameters.Add(productVersionParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static string GetEntityFrameworkProductVersion()
    {
        var informationalVersion = typeof(DbContext).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(informationalVersion))
            return typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "unknown";

        var metadataSeparatorIndex = informationalVersion.IndexOf('+');
        return metadataSeparatorIndex >= 0
            ? informationalVersion[..metadataSeparatorIndex]
            : informationalVersion;
    }

    private sealed record AuthSchemaState(string[] ExistingTables, string[] MissingTables);
}
