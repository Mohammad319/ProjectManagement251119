using Application.Feature.Calculation.Task;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Helper.Text;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;

namespace Persistence.Service.CalculationItems.Task;

public sealed class TaskResourceSuggestionService(
    IDbContextFactoryTenant dbFactory,
    IDbContextFactory<TaskResourceBlueprintsContext> blueprintFactory) : ITaskResourceSuggestionService
{
    private const double MinimumScore = 0.30d;
    private const int FallbackCandidateLimit = 500;
    private const int PriorityCandidateLimit = 1000;

    public async Task<IReadOnlyList<TaskResourceSuggestionDTO>> GetSuggestionsAsync(
        int taskId,
        int maxResults,
        bool includeResources = true,
        CancellationToken cancellationToken = default)
    {
        maxResults = Math.Clamp(maxResults, 1, 100);

        await using var tenantDb = await dbFactory.CreateDbContextAsync(cancellationToken);

        var target = await tenantDb.Tasks
            .AsNoTracking()
            .Where(x => x.Id == taskId)
            .FirstOrDefaultAsync(cancellationToken);

        if (target is null)
            return [];

        // Unit is excluded from the text so that compatible units (e.g. kg/ton) don't penalize Jaccard.
        // Unit scoring is handled separately in ScoreCandidate.
        var targetNormalized = SwedishTaskTextNormalizer.NormalizeTask(target.Name, target.Code, null);

        if (string.IsNullOrWhiteSpace(targetNormalized))
            return [];

        var tenantTask = GetTenantTaskSuggestionsAsync(
            tenantDb,
            target.Id,
            target.Name,
            targetNormalized,
            target.Unit,
            target.Code,
            target.Metadata.Quantity,
            maxResults,
            includeResources,
            cancellationToken);

        var blueprintTask = GetBlueprintSuggestionsAsync(
            tenantDb.TenantId,
            target.Name,
            targetNormalized,
            target.Unit,
            target.Code,
            target.Metadata.Quantity,
            maxResults,
            includeResources,
            cancellationToken);

        await System.Threading.Tasks.Task.WhenAll(tenantTask, blueprintTask);

        return (await tenantTask)
            .Concat(await blueprintTask)
            .Where(x => !includeResources || x.Resources.Count > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Source == TaskResourceSuggestionSource.BlueprintTask)
            .ThenBy(x => x.SourceTaskName)
            .Take(maxResults)
            .ToList();
    }

    public async Task<IReadOnlyList<ResourcePostDTO>> GetSuggestionResourcesAsync(
        int sourceTaskId,
        TaskResourceSuggestionSource source,
        CancellationToken cancellationToken = default)
    {
        return source switch
        {
            TaskResourceSuggestionSource.TenantTask => await GetTenantTaskResourcesAsync(sourceTaskId, cancellationToken),
            TaskResourceSuggestionSource.BlueprintTask => await GetBlueprintTaskResourcesAsync(sourceTaskId, cancellationToken),
            _ => []
        };
    }

    private static async Task<List<TaskResourceSuggestionDTO>> GetTenantTaskSuggestionsAsync(
        ShardingSingleDbContext db,
        int targetTaskId,
        string targetName,
        string targetNormalized,
        string? targetUnit,
        string? targetCode,
        decimal? targetQuantity,
        int maxResults,
        bool includeResources,
        CancellationToken ct)
    {
        var baseQuery = db.Tasks
            .AsNoTracking()
            .Where(x => x.Id != targetTaskId && x.Resources.Any());

        var priorityQuery = ApplyTenantPriorityFilter(baseQuery, targetName, targetCode, targetUnit)
            .OrderByDescending(x => x.Id)
            .Take(PriorityCandidateLimit);

        var fallbackQuery = baseQuery
            .OrderByDescending(x => x.Id)
            .Take(FallbackCandidateLimit);

        if (includeResources)
        {
            priorityQuery = priorityQuery.Include(x => x.Resources);
            fallbackQuery = fallbackQuery.Include(x => x.Resources);
        }

        var priorityCandidates = await priorityQuery.ToListAsync(ct);
        var fallbackCandidates = await fallbackQuery.ToListAsync(ct);

        var candidates = MergeCandidates(priorityCandidates, fallbackCandidates, x => x.Id);

        return candidates
            .Select(task =>
            {
                var candidateNormalized = SwedishTaskTextNormalizer.NormalizeTask(task.Name, task.Code, null);

                var score = ScoreCandidate(
                    targetNormalized,
                    candidateNormalized,
                    targetUnit,
                    task.Unit,
                    targetCode,
                    task.Code,
                    targetQuantity,
                    task.Metadata.Quantity,
                    targetName,
                    task.Name);

                return new TaskResourceSuggestionDTO
                {
                    SourceTaskId = task.Id,
                    SourceTaskName = task.Name,
                    SourceTaskQuantity = task.Metadata.Quantity,
                    SourceTaskUnit = task.Unit ?? string.Empty,
                    Source = TaskResourceSuggestionSource.TenantTask,
                    Score = score,
                    Reason = BuildReason(score, TaskResourceSuggestionSource.TenantTask),
                    Resources = includeResources
                        ? task.Resources
                            .OrderBy(x => x.SortOrder)
                            .Select(ToResourcePostDto)
                            .ToList()
                        : []
                };
            })
            .Where(x => x.Score >= MinimumScore)
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .ToList();
    }

    private async Task<List<TaskResourceSuggestionDTO>> GetBlueprintSuggestionsAsync(
        int tenantId,
        string targetName,
        string targetNormalized,
        string? targetUnit,
        string? targetCode,
        decimal? targetQuantity,
        int maxResults,
        bool includeResources,
        CancellationToken ct)
    {
        await using var db = await blueprintFactory.CreateDbContextAsync(ct);

        var baseQuery = db.Tasks
            .AsNoTracking()
            .Where(x => x.Status == TaskStatusEnum.Ready);

        var priorityQuery = ApplyBlueprintPriorityFilter(baseQuery, targetName, targetCode, targetUnit)
            .OrderBy(x => x.SortOrder)
            .Take(PriorityCandidateLimit);

        var fallbackQuery = baseQuery
            .OrderBy(x => x.SortOrder)
            .Take(FallbackCandidateLimit);

        var priorityCandidates = await priorityQuery.ToListAsync(ct);
        var fallbackCandidates = await fallbackQuery.ToListAsync(ct);

        var candidates = MergeCandidates(priorityCandidates, fallbackCandidates, x => x.Id);

        return candidates
            .Select(task =>
            {
                var candidateNormalized = SwedishTaskTextNormalizer.NormalizeTask(task.Name, task.Code, null);

                var score = ScoreCandidate(
                    targetNormalized,
                    candidateNormalized,
                    targetUnit,
                    task.UnitCode,
                    targetCode,
                    task.Code,
                    targetQuantity,
                    task.Quantity,
                    targetName,
                    task.Name);

                return new TaskResourceSuggestionDTO
                {
                    SourceTaskId = task.Id,
                    SourceTaskName = task.Name,
                    SourceTaskQuantity = task.Quantity,
                    SourceTaskUnit = task.UnitCode ?? string.Empty,
                    Source = TaskResourceSuggestionSource.BlueprintTask,
                    Score = score,
                    Reason = BuildReason(score, TaskResourceSuggestionSource.BlueprintTask),
                    Resources = []
                };
            })
            .Where(x => x.Score >= MinimumScore)
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .ToList();
    }

    private static IQueryable<TaskEntity> ApplyTenantPriorityFilter(
        IQueryable<TaskEntity> query,
        string targetName,
        string? targetCode,
        string? targetUnit)
    {
        var hasName = !string.IsNullOrWhiteSpace(targetName);
        var hasCode = !string.IsNullOrWhiteSpace(targetCode);
        var hasUnit = !string.IsNullOrWhiteSpace(targetUnit);
        var hasStrongMatch = hasName || hasCode;

        if (!hasName && !hasCode && !hasUnit)
            return query.Where(_ => false);

        return query.Where(x =>
            (hasName && x.Name == targetName) ||
            (hasCode && x.Code != null && x.Code == targetCode) ||
            (!hasStrongMatch && hasUnit && x.Unit != null && x.Unit == targetUnit));
    }

    private static IQueryable<TaskDefinition> ApplyBlueprintPriorityFilter(
        IQueryable<TaskDefinition> query,
        string targetName,
        string? targetCode,
        string? targetUnit)
    {
        var hasName = !string.IsNullOrWhiteSpace(targetName);
        var hasCode = !string.IsNullOrWhiteSpace(targetCode);
        var hasUnit = !string.IsNullOrWhiteSpace(targetUnit);
        var hasStrongMatch = hasName || hasCode;

        if (!hasName && !hasCode && !hasUnit)
            return query.Where(_ => false);

        return query.Where(x =>
            (hasName && x.Name == targetName) ||
            (hasCode && x.Code != null && x.Code == targetCode) ||
            (!hasStrongMatch && hasUnit && x.UnitCode != null && x.UnitCode == targetUnit));
    }

    private static List<T> MergeCandidates<T>(
        IReadOnlyList<T> priorityCandidates,
        IReadOnlyList<T> fallbackCandidates,
        Func<T, int> keySelector)
    {
        var seen = new HashSet<int>();
        var results = new List<T>(priorityCandidates.Count + fallbackCandidates.Count);

        foreach (var candidate in priorityCandidates.Concat(fallbackCandidates))
        {
            if (seen.Add(keySelector(candidate)))
                results.Add(candidate);
        }

        return results;
    }

    private async Task<IReadOnlyList<ResourcePostDTO>> GetTenantTaskResourcesAsync(
        int sourceTaskId,
        CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var task = await db.Tasks
            .AsNoTracking()
            .Where(x => x.Id == sourceTaskId)
            .Include(x => x.Resources)
            .FirstOrDefaultAsync(ct);

        if (task is null)
            return [];

        return task.Resources
            .OrderBy(x => x.SortOrder)
            .Select(ToResourcePostDto)
            .ToList();
    }

    private async Task<IReadOnlyList<ResourcePostDTO>> GetBlueprintTaskResourcesAsync(
        int sourceTaskId,
        CancellationToken ct)
    {
        await using var tenantDb = await dbFactory.CreateDbContextAsync(ct);
        var tenantId = tenantDb.TenantId;

        await using var db = await blueprintFactory.CreateDbContextAsync(ct);

        var task = await db.Tasks
            .AsNoTracking()
            .Where(x => x.Id == sourceTaskId && x.Status == TaskStatusEnum.Ready)
            .FirstOrDefaultAsync(ct);

        if (task is null)
            return [];

        return [];
    }

    private static double ScoreCandidate(
        string targetNormalized,
        string candidateNormalized,
        string? targetUnit,
        string? candidateUnit,
        string? targetCode = null,
        string? candidateCode = null,
        decimal? targetQuantity = null,
        decimal? candidateQuantity = null,
        string? targetName = null,
        string? candidateName = null)
    {
        var textScore = SwedishTaskTextNormalizer.CalculateSimilarity(targetNormalized, candidateNormalized);
        var nameScore = GetNameSimilarity(targetName, candidateName);
        var score = textScore;
        var quantitySimilarity = GetQuantitySimilarity(
            targetQuantity,
            targetUnit,
            candidateQuantity,
            candidateUnit);

        score = ApplyQuantityScore(score, quantitySimilarity);

        var hasEquivalentUnits = QuantityUnitNormalizer.AreEquivalentUnits(targetUnit, candidateUnit);
        var hasCompatibleUnits = QuantityUnitNormalizer.AreCompatibleUnits(targetUnit, candidateUnit);
        if (!string.IsNullOrWhiteSpace(targetUnit) && !string.IsNullOrWhiteSpace(candidateUnit))
        {
            if (hasEquivalentUnits)
                score = Math.Min(1d, score + 0.08d);
            else if (hasCompatibleUnits)
                score = Math.Min(1d, score + 0.06d);
        }

        var codeBonus = SwedishTaskTextNormalizer.GetCodeHierarchyScore(targetCode, candidateCode);
        score = Math.Min(1d, score + codeBonus);

        if (quantitySimilarity is >= 0.995d && (hasEquivalentUnits || hasCompatibleUnits))
        {
            if (nameScore >= 0.995d)
                score = Math.Max(score, codeBonus >= 0.15d ? (hasEquivalentUnits ? 1d : 0.99d) : 0.96d);
            else if (nameScore >= 0.90d && hasCompatibleUnits)
                score = Math.Max(score, codeBonus >= 0.15d ? 0.98d : 0.93d);
            else if (codeBonus >= 0.15d && textScore >= 0.45d)
                score = Math.Max(score, 0.93d);
            else if (textScore >= 0.75d)
                score = Math.Max(score, 0.90d);
        }

        return Math.Round(score, 4);
    }

    private static double GetNameSimilarity(string? targetName, string? candidateName)
    {
        var targetNameNormalized = SwedishTaskTextNormalizer.Normalize(targetName);
        var candidateNameNormalized = SwedishTaskTextNormalizer.Normalize(candidateName);

        return SwedishTaskTextNormalizer.CalculateSimilarity(targetNameNormalized, candidateNameNormalized);
    }

    private static double? GetQuantitySimilarity(
        decimal? targetQuantity, string? targetUnit,
        decimal? candidateQuantity, string? candidateUnit)
    {
        if (targetQuantity is not > 0m || candidateQuantity is not > 0m)
            return null;

        return QuantityUnitNormalizer.TryCalculateQuantitySimilarity(
            targetQuantity.Value,
            targetUnit,
            candidateQuantity.Value,
            candidateUnit,
            out var similarity)
                ? similarity
                : null;
    }

    private static double ApplyQuantityScore(double currentScore, double? quantitySimilarity)
    {
        if (quantitySimilarity is null)
            return currentScore;

        return Math.Clamp((currentScore * 0.85d) + (quantitySimilarity.Value * 0.15d), 0d, 1d);
    }

    private static string BuildReason(double score, TaskResourceSuggestionSource source)
    {
        var sourceText = source == TaskResourceSuggestionSource.BlueprintTask
            ? "TaskResourceBlueprints"
            : "Tenant task history";

        return score switch
        {
            >= 0.99d => $"Direct match from {sourceText}.",
            >= 0.90d => $"Very strong match from {sourceText}.",
            >= 0.75d => $"Strong match from {sourceText}.",
            >= 0.55d => $"Moderate match from {sourceText}.",
            _ => $"Weak match from {sourceText}."
        };
    }

    private static ResourcePostDTO ToResourcePostDto(ResourceEntity resource)
    {
        return new ResourcePostDTO
        {
            Name = resource.Name,
            IsActive = resource.IsActive,
            ResType = resource.ResType,
            AccountId = resource.AccountId,
            StatusId = resource.StatusId,
            ResourceSortId = resource.ResourceSortId,
            ResourceTypeId = resource.ResourceTypeId,
            OpportunityId = resource.OpportunityId,
            SortOrder = resource.SortOrder,
            Data = resource.GetMetadataSnapshot()
        };
    }

}
