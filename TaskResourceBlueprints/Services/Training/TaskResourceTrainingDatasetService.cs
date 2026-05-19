using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Helper.Text;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Training;

public interface ITaskResourceTrainingDatasetService
{
    Task<TaskResourceTrainingDataset> BuildAsync(
        int negativeExamplesPerPositive = 2,
        CancellationToken ct = default);
}

public sealed class TaskResourceTrainingDataset
{
    public int PositiveExamples { get; set; }
    public int NegativeExamples { get; set; }
    public int TaskCount { get; set; }
    public int ResourceCount { get; set; }
    public List<TaskResourceTrainingExample> Examples { get; set; } = [];
}

public sealed record TaskResourceTrainingExample(
    int TaskId,
    string TaskName,
    string? TaskCode,
    string? TaskUnit,
    decimal? TaskQuantity,
    int ResourceId,
    string ResourceName,
    string? ResourceUnit,
    decimal? ResourceQuantity,
    decimal LinkQuantity,
    bool Label,
    double TextSimilarity,
    double NameSimilarity,
    double QuantitySimilarity,
    bool HasQuantitySimilarity,
    bool HasSameUnit,
    bool HasCompatibleUnit,
    int TaskUsageCount);

public sealed class TaskResourceTrainingDatasetService(IDbContextFactory<TaskResourceBlueprintsContext> dbContextFactory)
    : ITaskResourceTrainingDatasetService
{
    public async Task<TaskResourceTrainingDataset> BuildAsync(
        int negativeExamplesPerPositive = 2,
        CancellationToken ct = default)
    {
        negativeExamplesPerPositive = Math.Clamp(negativeExamplesPerPositive, 0, 10);

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var links = await db.TaskDefinitionResourceLinks
            .AsNoTracking()
            .Include(x => x.Task)
            .Include(x => x.Resource)
            .Where(x => x.Task != null && x.Resource != null && x.Task.IsActive && x.Resource.IsActive && x.Resource.IsVisible)
            .ToListAsync(ct);
        var resources = await db.Resources
            .AsNoTracking()
            .Where(x => x.IsActive && x.IsVisible)
            .ToListAsync(ct);

        var dataset = new TaskResourceTrainingDataset
        {
            TaskCount = links.Select(x => x.TaskDefinitionId).Distinct().Count(),
            ResourceCount = resources.Count,
        };

        var linkedResourceIdsByTask = links
            .GroupBy(x => x.TaskDefinitionId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(link => link.ResourceDefinitionId).ToHashSet());

        foreach (var link in links)
        {
            ct.ThrowIfCancellationRequested();

            if (link.Task is null || link.Resource is null)
                continue;

            dataset.Examples.Add(CreateExample(link.Task, link.Resource, link.Quantity, label: true));
            dataset.PositiveExamples++;

            foreach (var negative in SelectNegativeResources(
                         link.Task,
                         resources,
                         linkedResourceIdsByTask[link.TaskDefinitionId],
                         negativeExamplesPerPositive))
            {
                dataset.Examples.Add(CreateExample(link.Task, negative, 0m, label: false));
                dataset.NegativeExamples++;
            }
        }

        await AddFeedbackExamplesAsync(db, dataset, ct);

        return dataset;
    }

    public static TaskResourceTrainingExample CreateExample(
        TaskDefinition task,
        ResourceDefinition resource,
        decimal linkQuantity,
        bool label)
    {
        var normalizedTask = string.IsNullOrWhiteSpace(task.NormalizedTextSv)
            ? SwedishTaskTextNormalizer.NormalizeTask(task.Name, task.Code, task.UnitCode, task.Quantity)
            : task.NormalizedTextSv;
        var normalizedResource = SwedishTaskTextNormalizer.NormalizeTask(
            resource.Name,
            null,
            resource.Unit,
            resource.Quantity);
        var normalizedResourceName = SwedishTaskTextNormalizer.Normalize(resource.Name);
        var nameSimilarity = SwedishTaskTextNormalizer.CalculateSimilarity(
            SwedishTaskTextNormalizer.Normalize(GetContextualTaskName(task)),
            normalizedResourceName);
        var textSimilarity = SwedishTaskTextNormalizer.CalculateSimilarity(normalizedTask, normalizedResource);
        var quantitySimilarity = CalculateQuantitySimilarity(task.Quantity, resource.Quantity);
        var hasSameUnit = HasSameUnit(task.UnitCode, resource.Unit);
        var hasCompatibleUnit = hasSameUnit || HasCompatibleUnit(task.UnitCode, resource.Unit);

        return new TaskResourceTrainingExample(
            task.Id,
            task.Name,
            task.Code,
            task.UnitCode,
            task.Quantity,
            resource.Id,
            resource.Name,
            resource.Unit,
            resource.Quantity,
            linkQuantity,
            label,
            textSimilarity,
            nameSimilarity,
            quantitySimilarity,
            task.Quantity.HasValue && resource.Quantity.HasValue,
            hasSameUnit,
            hasCompatibleUnit,
            task.UsageCount);
    }

    private static IEnumerable<ResourceDefinition> SelectNegativeResources(
        TaskDefinition task,
        IReadOnlyList<ResourceDefinition> resources,
        IReadOnlySet<int> linkedResourceIds,
        int count)
    {
        if (count <= 0 || resources.Count == 0)
            return [];

        var selected = new List<ResourceDefinition>(count);
        var start = Math.Abs(HashCode.Combine(task.Id, task.Code, task.Name)) % resources.Count;

        for (var offset = 0; offset < resources.Count && selected.Count < count; offset++)
        {
            var resource = resources[(start + offset) % resources.Count];
            if (linkedResourceIds.Contains(resource.Id))
                continue;

            selected.Add(resource);
        }

        return selected;
    }

    private static async Task AddFeedbackExamplesAsync(
        TaskResourceBlueprintsContext db,
        TaskResourceTrainingDataset dataset,
        CancellationToken ct)
    {
        var feedbacks = await db.TaskResourceSuggestionFeedbacks
            .AsNoTracking()
            .Where(x =>
                x.Source == TaskResourceSuggestionSource.BlueprintTask &&
                x.ReviewStatus == TaskResourceSuggestionFeedbackReviewStatus.Approved &&
                (x.Feedback == TaskResourceSuggestionFeedbackKind.Accepted ||
                 x.Feedback == TaskResourceSuggestionFeedbackKind.Rejected ||
                 x.Feedback == TaskResourceSuggestionFeedbackKind.WrongUnit ||
                 x.Feedback == TaskResourceSuggestionFeedbackKind.WrongResourceType))
            .ToListAsync(ct);

        if (feedbacks.Count == 0)
            return;

        var sourceTaskIds = feedbacks.Select(x => x.SourceTaskId).Distinct().ToList();
        var sourceLinks = await db.TaskDefinitionResourceLinks
            .AsNoTracking()
            .Include(x => x.Resource)
            .Where(x => sourceTaskIds.Contains(x.TaskDefinitionId) && x.Resource != null && x.Resource.IsActive && x.Resource.IsVisible)
            .ToListAsync(ct);
        var linksBySourceTask = sourceLinks
            .GroupBy(x => x.TaskDefinitionId)
            .ToDictionary(x => x.Key, x => x.ToList());

        foreach (var feedback in feedbacks)
        {
            ct.ThrowIfCancellationRequested();

            if (!linksBySourceTask.TryGetValue(feedback.SourceTaskId, out var feedbackSourceLinks))
                continue;

            var targetTask = CreateFeedbackTargetTask(feedback);
            var label = feedback.Feedback == TaskResourceSuggestionFeedbackKind.Accepted;

            foreach (var link in feedbackSourceLinks)
            {
                if (link.Resource is null)
                    continue;

                dataset.Examples.Add(CreateExample(targetTask, link.Resource, link.Quantity, label));
                if (label)
                    dataset.PositiveExamples++;
                else
                    dataset.NegativeExamples++;
            }
        }
    }

    private static TaskDefinition CreateFeedbackTargetTask(TaskResourceSuggestionFeedback feedback)
    {
        var task = new TaskDefinition
        {
            Id = -feedback.Id,
            Name = feedback.TargetTaskName,
            Code = feedback.TargetTaskCode,
            UnitCode = feedback.TargetTaskUnit,
            Quantity = feedback.TargetTaskQuantity,
            UsageCount = 1
        };
        task.RefreshNormalizedTextSv();
        return task;
    }

    private static string GetContextualTaskName(TaskDefinition task)
    {
        if (string.IsNullOrWhiteSpace(task.HierarchyPath))
            return task.Name;

        var names = task.HierarchyPath
            .Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ExtractNameFromHierarchyPart)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0 || !names.Any(x => string.Equals(x, task.Name, StringComparison.OrdinalIgnoreCase)))
            names.Add(task.Name);

        return string.Join(' ', names);
    }

    private static string ExtractNameFromHierarchyPart(string part)
    {
        var trimmed = part.Trim();
        var firstSpace = trimmed.IndexOf(' ');
        return firstSpace > 0 && trimmed[..firstSpace].Any(char.IsLetterOrDigit)
            ? trimmed[(firstSpace + 1)..].Trim()
            : trimmed;
    }

    private static double CalculateQuantitySimilarity(decimal? taskQuantity, decimal? resourceQuantity)
    {
        if (!taskQuantity.HasValue || !resourceQuantity.HasValue)
            return 0d;

        var max = Math.Max(Math.Abs(taskQuantity.Value), Math.Abs(resourceQuantity.Value));
        if (max <= 0m)
            return 1d;

        var diff = Math.Abs(taskQuantity.Value - resourceQuantity.Value);
        return Math.Round((double)Math.Clamp(1m - (diff / max), 0m, 1m), 4);
    }

    private static bool HasSameUnit(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left) &&
           !string.IsNullOrWhiteSpace(right) &&
           string.Equals(NormalizeUnit(left), NormalizeUnit(right), StringComparison.OrdinalIgnoreCase);

    private static bool HasCompatibleUnit(string? left, string? right)
    {
        var normalizedLeft = NormalizeUnit(left);
        var normalizedRight = NormalizeUnit(right);
        if (string.IsNullOrWhiteSpace(normalizedLeft) || string.IsNullOrWhiteSpace(normalizedRight))
            return false;

        return UnitFamily(normalizedLeft) == UnitFamily(normalizedRight);
    }

    private static string NormalizeUnit(string? unit)
        => string.IsNullOrWhiteSpace(unit)
            ? string.Empty
            : SwedishTaskTextNormalizer.Normalize(unit).Replace("²", "2").Replace("³", "3");

    private static string UnitFamily(string unit)
        => unit switch
        {
            "m" or "lm" => "length",
            "m2" or "kvm" => "area",
            "m3" or "kbm" => "volume",
            "st" or "pcs" => "count",
            "h" or "tim" or "dag" => "time",
            "kg" or "ton" => "weight",
            _ => unit
        };
}
