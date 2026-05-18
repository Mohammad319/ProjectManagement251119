using TaskResourceBlueprints.Entities.Tasks;

namespace TaskResourceBlueprints.Services.Import;

internal static class TaskHierarchyContextBuilder
{
    public static void Apply(
        TaskDefinition task,
        string? parentCode,
        IReadOnlyDictionary<string, TaskDefinition> taskByCode)
    {
        var parent = ResolveParentTask(parentCode, task.Code, taskByCode);
        if (parent is null)
        {
            task.ParentCode = parentCode;
            task.ParentName = null;
            task.HierarchyPath = FormatHierarchyPart(task.Code, task.Name);
            return;
        }

        task.ParentCode = string.IsNullOrWhiteSpace(parent.Code) ? parentCode : parent.Code.Trim();
        task.ParentName = parent.Name;
        task.HierarchyPath = string.IsNullOrWhiteSpace(parent.HierarchyPath)
            ? $"{FormatHierarchyPart(parent.Code, parent.Name)} > {FormatHierarchyPart(task.Code, task.Name)}"
            : $"{parent.HierarchyPath} > {FormatHierarchyPart(task.Code, task.Name)}";
    }

    public static string? InferParentCode(string? code, IEnumerable<string> knownCodes)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var trimmedCode = code.Trim();
        return knownCodes
            .Where(candidate => IsLikelyParentCode(trimmedCode, candidate))
            .OrderByDescending(candidate => candidate.Length)
            .FirstOrDefault();
    }

    private static TaskDefinition? ResolveParentTask(
        string? parentCode,
        string? taskCode,
        IReadOnlyDictionary<string, TaskDefinition> taskByCode)
    {
        if (!string.IsNullOrWhiteSpace(parentCode) &&
            taskByCode.TryGetValue(parentCode.Trim(), out var byCode))
            return byCode;

        var inferredParentCode = InferParentCode(taskCode, taskByCode.Keys);
        return !string.IsNullOrWhiteSpace(inferredParentCode) &&
               taskByCode.TryGetValue(inferredParentCode, out var inferredParent)
            ? inferredParent
            : null;
    }

    private static bool IsLikelyParentCode(string code, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        var parent = candidate.Trim();
        if (parent.Length >= code.Length ||
            !code.StartsWith(parent, StringComparison.OrdinalIgnoreCase))
            return false;

        var next = code[parent.Length];
        return next == '.' || char.IsLetterOrDigit(next);
    }

    private static string FormatHierarchyPart(string? code, string name)
        => string.IsNullOrWhiteSpace(code) ? name.Trim() : $"{code.Trim()} {name.Trim()}";
}
