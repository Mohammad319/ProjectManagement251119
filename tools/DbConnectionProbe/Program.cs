using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.SqlClient;

var options = CliOptions.Parse(args);

if (options.ShowHelp)
{
    CliOptions.PrintHelp();
    return 0;
}

try
{
    await RunAsync(options);
    return 0;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Fatal error");
    Console.ResetColor();
    Console.WriteLine(ex);
    return 1;
}

static async Task RunAsync(CliOptions options)
{
    var targets = new List<ConnectionTarget>();

    if (!string.IsNullOrWhiteSpace(options.AppSettingsPath))
    {
        var fromSettings = await AppSettingsReader.ReadAsync(options.AppSettingsPath);

        if (!string.IsNullOrWhiteSpace(fromSettings.AuthPermissionsConnection))
            targets.Add(new ConnectionTarget("AuthPermissions", fromSettings.AuthPermissionsConnection));

        if (!string.IsNullOrWhiteSpace(fromSettings.BlueprintsConnection))
            targets.Add(new ConnectionTarget("Blueprints", fromSettings.BlueprintsConnection));
    }

    if (!string.IsNullOrWhiteSpace(options.AuthConnectionString))
        targets.Add(new ConnectionTarget("AuthPermissions (Override)", options.AuthConnectionString));

    if (!string.IsNullOrWhiteSpace(options.BlueprintsConnectionString))
        targets.Add(new ConnectionTarget("Blueprints (Override)", options.BlueprintsConnectionString));

    foreach (var item in options.ManualConnections)
        targets.Add(item);

    if (targets.Count == 0)
        throw new InvalidOperationException("No connection strings were provided. Use --appsettings or --connection.");

    ConnectionTarget? authTarget = targets
        .LastOrDefault(x => x.Name.StartsWith("AuthPermissions", StringComparison.OrdinalIgnoreCase));

    foreach (var target in targets)
    {
        Console.WriteLine(new string('=', 80));
        await ProbeConnectionAsync(target, options.ShowSecrets);
        Console.WriteLine();
    }

    if (options.TenantId is > 0)
    {
        if (authTarget is null)
            throw new InvalidOperationException("A tenant lookup requires AuthPermissions connection. Provide --appsettings or --auth-conn.");

        Console.WriteLine(new string('=', 80));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Tenant Mapping Lookup: TenantId = {options.TenantId}");
        Console.ResetColor();

        var tenantInfo = await TenantLookup.LoadAsync(authTarget.ConnectionString, options.TenantId.Value);
        if (tenantInfo is null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Tenant was not found in AuthPermissions database.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"Tenant Name      : {tenantInfo.TenantName}");
        Console.WriteLine($"TenantDBId       : {tenantInfo.TenantDbId}");
        Console.WriteLine($"Database Name    : {tenantInfo.DatabaseName}");
        Console.WriteLine($"ConnectionString : {ConnectionStringDisplay.Format(tenantInfo.ConnectionString, options.ShowSecrets)}");
        Console.WriteLine();

        await ProbeConnectionAsync(
            new ConnectionTarget($"Tenant {tenantInfo.TenantId} -> {tenantInfo.DatabaseName}", tenantInfo.ConnectionString),
            options.ShowSecrets);
    }
}

static async Task ProbeConnectionAsync(ConnectionTarget target, bool showSecrets)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(target.Name);
    Console.ResetColor();

    Console.WriteLine($"ConnectionString : {ConnectionStringDisplay.Format(target.ConnectionString, showSecrets)}");

    SqlConnectionStringBuilder? builder = null;
    try
    {
        builder = new SqlConnectionStringBuilder(target.ConnectionString);
        Console.WriteLine($"DataSource       : {builder.DataSource}");
        Console.WriteLine($"InitialCatalog   : {builder.InitialCatalog}");
        Console.WriteLine($"IntegratedSec    : {builder.IntegratedSecurity}");
        Console.WriteLine($"Encrypt          : {builder.Encrypt}");
        Console.WriteLine($"TrustServerCert  : {builder.TrustServerCertificate}");
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Unable to parse connection string metadata: {ex.Message}");
        Console.ResetColor();
    }

    var watch = Stopwatch.StartNew();
    try
    {
        await using var connection = new SqlConnection(target.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName";

        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();

        watch.Stop();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Result           : SUCCESS ({watch.ElapsedMilliseconds} ms)");
        Console.ResetColor();
        Console.WriteLine($"Resolved Server  : {reader["ServerName"]}");
        Console.WriteLine($"Resolved DB      : {reader["DatabaseName"]}");
    }
    catch (SqlException ex)
    {
        watch.Stop();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Result           : FAILED ({watch.ElapsedMilliseconds} ms)");
        Console.ResetColor();
        Console.WriteLine($"Exception        : {ex.GetType().Name}");
        Console.WriteLine($"SqlNumber        : {ex.Number}");
        Console.WriteLine($"State            : {ex.State}");
        Console.WriteLine($"Class            : {ex.Class}");
        Console.WriteLine($"Message          : {ex.Message}");
    }
    catch (Exception ex)
    {
        watch.Stop();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Result           : FAILED ({watch.ElapsedMilliseconds} ms)");
        Console.ResetColor();
        Console.WriteLine($"Exception        : {ex.GetType().Name}");
        Console.WriteLine($"Message          : {ex.Message}");
    }
}

sealed record ConnectionTarget(string Name, string ConnectionString);

sealed record TenantInfo(
    int TenantId,
    string TenantName,
    int TenantDbId,
    string DatabaseName,
    string ConnectionString);

sealed class CliOptions
{
    public string? AppSettingsPath { get; private set; }
    public string? AuthConnectionString { get; private set; }
    public string? BlueprintsConnectionString { get; private set; }
    public int? TenantId { get; private set; }
    public bool ShowSecrets { get; private set; }
    public bool ShowHelp { get; private set; }
    public List<ConnectionTarget> ManualConnections { get; } = [];

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "--help":
                case "-h":
                case "/?":
                    options.ShowHelp = true;
                    break;
                case "--show-secrets":
                    options.ShowSecrets = true;
                    break;
                case "--appsettings":
                    options.AppSettingsPath = RequireValue(args, ref i, arg);
                    break;
                case "--auth-conn":
                    options.AuthConnectionString = RequireValue(args, ref i, arg);
                    break;
                case "--blueprints-conn":
                    options.BlueprintsConnectionString = RequireValue(args, ref i, arg);
                    break;
                case "--tenant-id":
                    if (!int.TryParse(RequireValue(args, ref i, arg), out var tenantId) || tenantId <= 0)
                        throw new ArgumentException("Tenant id must be a positive integer.");

                    options.TenantId = tenantId;
                    break;
                case "--connection":
                {
                    var raw = RequireValue(args, ref i, arg);
                    var splitIndex = raw.IndexOf('=');
                    if (splitIndex <= 0 || splitIndex == raw.Length - 1)
                        throw new ArgumentException("--connection must be in the form Name=ConnectionString");

                    var name = raw[..splitIndex].Trim();
                    var connectionString = raw[(splitIndex + 1)..].Trim();
                    options.ManualConnections.Add(new ConnectionTarget(name, connectionString));
                    break;
                }
                default:
                    throw new ArgumentException($"Unknown argument '{arg}'. Use --help.");
            }
        }

        return options;
    }

    public static void PrintHelp()
    {
        Console.WriteLine("""
DbConnectionProbe

Usage:
  dotnet run --project tools/DbConnectionProbe -- --appsettings ProjectManagement/ProjectManagement/appsettings.json --tenant-id 1
  dotnet run --project tools/DbConnectionProbe -- --auth-conn "<AUTH_CONNECTION>" --tenant-id 1
  dotnet run --project tools/DbConnectionProbe -- --connection "TenantDb=Server=.;Database=PM_Tenant_DB1;Integrated Security=True"

Options:
  --appsettings <path>      Read AuthPermissionsConnection and BlueprintsConnection from appsettings.json
  --auth-conn <string>      AuthPermissions connection string
  --blueprints-conn <str>   Blueprints connection string
  --tenant-id <id>          Load tenant mapping from AuthPermissions and probe that tenant database
  --connection Name=Conn    Probe a manual connection string (can be used multiple times)
  --show-secrets            Print connection strings without masking passwords
  --help                    Show this help
""");
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
            throw new ArgumentException($"Missing value for {option}");

        index++;
        return args[index];
    }
}

static class AppSettingsReader
{
    public static async Task<AppSettingsConnections> ReadAsync(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("appsettings.json was not found.", path);

        await using var stream = File.OpenRead(path);
        using var document = await JsonDocument.ParseAsync(stream);

        if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
            return new AppSettingsConnections(null, null);

        return new AppSettingsConnections(
            ReadString(connectionStrings, "AuthPermissionsConnection"),
            ReadString(connectionStrings, "BlueprintsConnection"));
    }

    private static string? ReadString(JsonElement parent, string propertyName)
    {
        return parent.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}

sealed record AppSettingsConnections(string? AuthPermissionsConnection, string? BlueprintsConnection);

static class TenantLookup
{
    public static async Task<TenantInfo?> LoadAsync(string authConnectionString, int tenantId)
    {
        const string sql = """
            SELECT TOP (1)
                t.Id,
                t.Name,
                t.TenantDBId,
                db.Name AS DatabaseName,
                db.ConnectionString
            FROM Tenants t
            LEFT JOIN TenantDatabase db ON db.Id = t.TenantDBId
            WHERE t.Id = @tenantId
            """;

        await using var connection = new SqlConnection(authConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@tenantId", tenantId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new TenantInfo(
            TenantId: reader.GetInt32(reader.GetOrdinal("Id")),
            TenantName: reader.GetString(reader.GetOrdinal("Name")),
            TenantDbId: reader.GetInt32(reader.GetOrdinal("TenantDBId")),
            DatabaseName: reader.IsDBNull(reader.GetOrdinal("DatabaseName"))
                ? "(null)"
                : reader.GetString(reader.GetOrdinal("DatabaseName")),
            ConnectionString: reader.IsDBNull(reader.GetOrdinal("ConnectionString"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("ConnectionString")));
    }
}

static class ConnectionStringDisplay
{
    public static string Format(string connectionString, bool showSecrets)
    {
        if (showSecrets)
            return connectionString;

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

            if (builder.ContainsKey("Password"))
                builder.Password = "***";

            if (builder.ContainsKey("User ID") && !builder.IntegratedSecurity)
                builder.UserID = Mask(builder.UserID);

            return builder.ConnectionString;
        }
        catch
        {
            return connectionString;
        }
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= 2)
            return "***";

        return $"{value[0]}***{value[^1]}";
    }
}
