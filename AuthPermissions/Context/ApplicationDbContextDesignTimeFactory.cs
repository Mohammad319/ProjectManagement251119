using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthPermissions.Context;

public sealed class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static string ResolveConnectionString()
    {
        return FirstNonEmpty(
                   Environment.GetEnvironmentVariable("ConnectionStrings__PMTConnection"),
                   Environment.GetEnvironmentVariable("ConnectionStrings__PMPConnection"),
                   Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"),
                   TryReadConnectionStringFromSettings())
               ?? throw new InvalidOperationException(
                   "No SQL Server connection string was found for ApplicationDbContext. " +
                   "Set 'ConnectionStrings__PMTConnection', 'ConnectionStrings__PMPConnection', or fallback 'ConnectionStrings__DefaultConnection', " +
                   "or define 'PMTConnection', 'PMPConnection', or 'DefaultConnection' in an appsettings file.");
    }

    private static string? TryReadConnectionStringFromSettings()
    {
        var environmentName =
            FirstNonEmpty(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"))
            ?? "Development";

        string? connectionString = null;
        foreach (var settingsPath in EnumerateSettingsFiles(environmentName))
        {
            var value = TryReadConnectionString(settingsPath);
            if (!string.IsNullOrWhiteSpace(value))
                connectionString = value;
        }

        return connectionString;
    }

    private static IEnumerable<string> EnumerateSettingsFiles(string environmentName)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in EnumerateSearchRoots())
        {
            foreach (var relativePath in new[]
                     {
                         "appsettings.json",
                         $"appsettings.{environmentName}.json",
                         Path.Combine("ProjectManagement", "ProjectManagement", "appsettings.json"),
                         Path.Combine("ProjectManagement", "ProjectManagement", $"appsettings.{environmentName}.json"),
                         Path.Combine("ProjectManagement.Adminstrator", "appsettings.json"),
                         Path.Combine("ProjectManagement.Adminstrator", $"appsettings.{environmentName}.json")
                     })
            {
                var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
                if (File.Exists(fullPath) && seen.Add(fullPath))
                    yield return fullPath;
            }
        }
    }

    private static IEnumerable<string> EnumerateSearchRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            if (string.IsNullOrWhiteSpace(start))
                continue;

            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (seen.Add(directory.FullName))
                    yield return directory.FullName;

                directory = directory.Parent;
            }
        }
    }

    private static string? TryReadConnectionString(string settingsPath)
    {
        using var stream = File.OpenRead(settingsPath);
        using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
            return null;

        return ReadConnectionString(connectionStrings, "PMTConnection")
               ?? ReadConnectionString(connectionStrings, "PMPConnection")
               ?? ReadConnectionString(connectionStrings, "DefaultConnection");
    }

    private static string? ReadConnectionString(JsonElement connectionStrings, string key)
    {
        if (!connectionStrings.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.String)
            return null;

        return value.GetString();
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
