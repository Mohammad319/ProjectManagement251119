using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Helper.Text;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Evaluation;

public interface ITaskResourceSuggestionEvaluationService
{
    Task<TaskResourceSuggestionEvaluationResult> EvaluateAsync(
        int sampleSize = 100,
        int topK = 5,
        CancellationToken ct = default);
}

public sealed class TaskResourceSuggestionEvaluationResult
{
    public int EligibleTasks { get; set; }
    public int EvaluatedTasks { get; set; }
    public int TopK { get; set; }
    public int Top1Hits { get; set; }
    public int TopKHits { get; set; }
    public int UnitMatches { get; set; }
    public int TypeMatches { get; set; }
    public double AverageTop1ResourceOverlap { get; set; }
    public double AverageBestTopKResourceOverlap { get; set; }
    public List<TaskResourceSuggestionEvaluationItem> Items { get; set; } = [];

    public double Top1HitRate => Rate(Top1Hits, EvaluatedTasks);
    public double TopKHitRate => Rate(TopKHits, EvaluatedTasks);
    public double UnitMatchRate => Rate(UnitMatches, EvaluatedTasks);
    public double TypeMatchRate => Rate(TypeMatches, EvaluatedTasks);

    private static double Rate(int value, int total)
        => total <= 0 ? 0d : Math.Round(value / (double)total, 4);
}

public sealed class TaskResourceSuggestionEvaluationItem
{
    public int TaskId { get; set; }
    public string TaskCode { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string TaskUnit { get; set; } = string.Empty;
    public int ExpectedResourceCount { get; set; }
    public string? Top1TaskCode { get; set; }
    public string? Top1TaskName { get; set; }
    public double Top1Score { get; set; }
    public double Top1ResourceOverlap { get; set; }
    public double BestTopKResourceOverlap { get; set; }
    public bool Top1Hit { get; set; }
    public bool TopKHit { get; set; }
    public bool UnitMatch { get; set; }
    public bool TypeMatch { get; set; }
}

public sealed class TaskResourceSuggestionEvaluationService(IDbContextFactory<TaskResourceBlueprintsContext> dbContextFactory)
    : ITaskResourceSuggestionEvaluationService
{
    private const double EquivalentUnitBonus = 0.15d;
    private const double CompatibleUnitBonus = 0.10d;
    private const double IncompatibleUnitPenalty = 0.04d;

    public async Task<TaskResourceSuggestionEvaluationResult> EvaluateAsync(
        int sampleSize = 100,
        int topK = 5,
        CancellationToken ct = default)
    {
        sampleSize = Math.Clamp(sampleSize, 1, 500);
        topK = Math.Clamp(topK, 1, 20);

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var tasks = await db.Tasks
            .AsNoTracking()
            .Where(x => x.Status == TaskStatusEnum.Ready && x.IsActive && x.IsVisible)
            .Include(x => x.ResourceLinks)
                .ThenInclude(x => x.Resource)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        var eligible = tasks
            .Where(x => GetActiveResourceLinks(x).Count > 0)
            .ToList();

        var result = new TaskResourceSuggestionEvaluationResult
        {
            EligibleTasks = eligible.Count,
            TopK = topK,
        };

        foreach (var target in eligible.Take(sampleSize))
        {
            ct.ThrowIfCancellationRequested();

            var targetLinks = GetActiveResourceLinks(target);
            var ranked = eligible
                .Where(candidate => candidate.Id != target.Id)
                .Select(candidate => new
                {
                    Task = candidate,
                    Score = ScoreTask(target, candidate),
                    Links = GetActiveResourceLinks(candidate),
                })
                .Where(x => x.Links.Count > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Task.SortOrder)
                .Take(topK)
                .ToList();

            if (ranked.Count == 0)
                continue;

            var top1 = ranked[0];
            var top1Overlap = CalculateResourceOverlap(targetLinks, top1.Links);
            var bestTopKOverlap = ranked.Max(x => CalculateResourceOverlap(targetLinks, x.Links));
            var top1Hit = top1Overlap > 0d;
            var topKHit = bestTopKOverlap > 0d;
            var unitMatch = QuantityUnitNormalizer.AreCompatibleUnits(target.UnitCode, top1.Task.UnitCode);
            var typeMatch = HasResourceTypeOverlap(targetLinks, top1.Links);

            result.Items.Add(new TaskResourceSuggestionEvaluationItem
            {
                TaskId = target.Id,
                TaskCode = target.Code ?? string.Empty,
                TaskName = target.Name,
                TaskUnit = target.UnitCode ?? string.Empty,
                ExpectedResourceCount = targetLinks.Count,
                Top1TaskCode = top1.Task.Code,
                Top1TaskName = top1.Task.Name,
                Top1Score = top1.Score,
                Top1ResourceOverlap = top1Overlap,
                BestTopKResourceOverlap = bestTopKOverlap,
                Top1Hit = top1Hit,
                TopKHit = topKHit,
                UnitMatch = unitMatch,
                TypeMatch = typeMatch
            });

            if (top1Hit) result.Top1Hits++;
            if (topKHit) result.TopKHits++;
            if (unitMatch) result.UnitMatches++;
            if (typeMatch) result.TypeMatches++;
        }

        result.EvaluatedTasks = result.Items.Count;
        result.AverageTop1ResourceOverlap = Math.Round(result.Items.Select(x => x.Top1ResourceOverlap).DefaultIfEmpty(0d).Average(), 4);
        result.AverageBestTopKResourceOverlap = Math.Round(result.Items.Select(x => x.BestTopKResourceOverlap).DefaultIfEmpty(0d).Average(), 4);
        return result;
    }

    private static List<TaskDefinitionResourceLink> GetActiveResourceLinks(TaskDefinition task)
        => task.ResourceLinks
            .Where(x => x.Resource is { IsActive: true, IsVisible: true })
            .ToList();

    private static double ScoreTask(TaskDefinition target, TaskDefinition candidate)
    {
        var targetNormalized = GetNormalizedTaskText(target);
        var candidateNormalized = GetNormalizedTaskText(candidate);
        var textScore = SwedishTaskTextNormalizer.CalculateSimilarity(targetNormalized, candidateNormalized);
        var nameScore = SwedishTaskTextNormalizer.CalculateSimilarity(
            SwedishTaskTextNormalizer.Normalize(target.Name),
            SwedishTaskTextNormalizer.Normalize(candidate.Name));
        var score = Math.Max(textScore, (textScore * 0.55d) + (nameScore * 0.45d));

        if (!string.IsNullOrWhiteSpace(target.UnitCode) && !string.IsNullOrWhiteSpace(candidate.UnitCode))
        {
            if (QuantityUnitNormalizer.AreEquivalentUnits(target.UnitCode, candidate.UnitCode))
                score += EquivalentUnitBonus;
            else if (QuantityUnitNormalizer.AreCompatibleUnits(target.UnitCode, candidate.UnitCode))
                score += CompatibleUnitBonus;
            else
                score -= IncompatibleUnitPenalty;
        }

        score += SwedishTaskTextNormalizer.GetCodeHierarchyScore(target.Code, candidate.Code);
        return Math.Round(Math.Clamp(score, 0d, 1d), 4);
    }

    private static string GetNormalizedTaskText(TaskDefinition task)
        => string.IsNullOrWhiteSpace(task.NormalizedTextSv)
            ? SwedishTaskTextNormalizer.NormalizeTask(task.Name, task.Code, null)
            : task.NormalizedTextSv;

    private static double CalculateResourceOverlap(
        IReadOnlyList<TaskDefinitionResourceLink> expected,
        IReadOnlyList<TaskDefinitionResourceLink> actual)
    {
        if (expected.Count == 0 || actual.Count == 0)
            return 0d;

        var actualKeys = actual
            .Select(x => NormalizeResourceName(x.Resource!.Name))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hits = expected.Count(x => actualKeys.Contains(NormalizeResourceName(x.Resource!.Name)));
        return Math.Round(hits / (double)expected.Count, 4);
    }

    private static bool HasResourceTypeOverlap(
        IReadOnlyList<TaskDefinitionResourceLink> expected,
        IReadOnlyList<TaskDefinitionResourceLink> actual)
    {
        var actualTypes = actual.Select(x => x.Resource!.ResType).ToHashSet();
        return expected.Any(x => actualTypes.Contains(x.Resource!.ResType));
    }

    private static string NormalizeResourceName(string name)
        => SwedishTaskTextNormalizer.Normalize(name);
}
