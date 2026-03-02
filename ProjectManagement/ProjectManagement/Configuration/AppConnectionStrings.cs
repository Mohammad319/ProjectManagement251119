namespace ProjectManagement.Configuration;

public sealed record AppConnectionStrings(string DefaultConnection, string TaskResourceBlueprintsDb);

public static class AppConnectionStringsReader
{
    public static AppConnectionStrings Read(IConfiguration config)
    {
        var taskResourceBlueprintsConnection = config.GetConnectionString("BlueprintsDB")
            ?? throw new InvalidOperationException("Connection string 'BlueprintsDB' not found.");

        var defaultConnection = config.GetConnectionString("AuthPermissionsDB")
            ?? config.GetConnectionString("AuthPermissions")
            ?? config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'AuthPermissionsDB' not found.");

        return new AppConnectionStrings(defaultConnection, taskResourceBlueprintsConnection);
    }
}
