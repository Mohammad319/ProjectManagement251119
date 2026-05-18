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
}
