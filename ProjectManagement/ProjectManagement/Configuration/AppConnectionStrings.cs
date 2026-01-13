namespace ProjectManagement.Configuration;

public sealed record AppConnectionStrings(string DefaultConnection, string TaskResourceBlueprintsDb);

public static class AppConnectionStringsReader
{
    public static AppConnectionStrings Read(IConfiguration config)
    {
        var TaskResourceBlueprintsConnection = config.GetConnectionString("TaskResourceBlueprintsConnection")
            ?? throw new InvalidOperationException("Connection string 'TaskResourceBlueprintsConnection' not found.");

        var defaultConnection = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        return new AppConnectionStrings(defaultConnection, TaskResourceBlueprintsConnection);
    }
}
