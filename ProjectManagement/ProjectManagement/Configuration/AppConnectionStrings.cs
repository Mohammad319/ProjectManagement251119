namespace ProjectManagement.Configuration;

public sealed record AppConnectionStrings(string DefaultConnection, string TaskResourceBlueprintsDb);

public static class AppConnectionStringsReader
{
    public static AppConnectionStrings Read(IConfiguration config)
    {
        var taskResourceBlueprintsConnection =
            config.GetConnectionString("TaskResourceBlueprintsConnection")
            ?? config.GetConnectionString("TaskResourceBlueprintsDb")
            ?? throw new InvalidOperationException("Connection string 'TaskResourceBlueprintsConnection' (or 'TaskResourceBlueprintsDb') not found.");

        var defaultConnection = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        return new AppConnectionStrings(defaultConnection, taskResourceBlueprintsConnection);
    }
}
