namespace ProjectManagement.Shared.Helper.ML;

public static class TaskResourceSuggestionMlModelPath
{
    private const string ModelFileName = "task-resource-suggestions.zip";

    public static string GetDefaultModelPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        if (string.IsNullOrWhiteSpace(root))
            root = AppContext.BaseDirectory;

        return Path.Combine(root, "ProjectManagement", "ML", ModelFileName);
    }

    public static string GetTenantModelPath(int tenantId)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId));

        var root = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        if (string.IsNullOrWhiteSpace(root))
            root = AppContext.BaseDirectory;

        return Path.Combine(root, "ProjectManagement", "ML", "tenants", tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture), ModelFileName);
    }
}
