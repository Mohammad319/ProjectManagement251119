namespace ProjectManagement.Configuration;

public sealed record AppConnectionStrings(string DefaultConnection, string TaskResourceBlueprintsDb);

public static class AppConnectionStringsReader
{
    public static AppConnectionStrings Read(IConfiguration config)
    {
        var taskResourceBlueprintsConnection =
            config.GetConnectionString("BlueprintsConnection")
            ?? config.GetConnectionString("BlueprintsConnection")
            ?? throw new InvalidOperationException("Connection string 'BlueprintsConnection' (or 'BlueprintsConnection') not found.");

        var defaultConnection =
            config.GetConnectionString("AuthPermissionsConnection")
            ?? config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'AuthPermissionsConnection' (or fallback 'DefaultConnection') not found.");

        return new AppConnectionStrings(defaultConnection, taskResourceBlueprintsConnection);
    }
}
