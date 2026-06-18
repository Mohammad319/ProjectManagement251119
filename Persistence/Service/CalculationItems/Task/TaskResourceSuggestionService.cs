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
    IDbContextFactory<TaskResourceBlueprintsContext> blueprintFactory,
    ITaskResourceSuggestionMlRanker mlRanker) : ITaskResourceSuggestionService
{
    private const double MinimumScore = 0.30d;
    private const int FallbackCandidateLimit = 500;
    private const int PriorityCandidateLimit = 1000;
    private const double EquivalentUnitBonus = 0.15d;
    private const double CompatibleUnitBonus = 0.10d;
    private const double IncompatibleUnitPenalty = 0.04d;
    private const double ParentContextBonus = 0.04d;
    private static readonly SourceScoreProfile DefaultScoreProfile = new(0.18d, EquivalentUnitBonus, CompatibleUnitBonus, IncompatibleUnitPenalty, 1d, 1d);
    private static readonly SourceScoreProfile TenantScoreProfile = new(0.22d, 0.13d, 0.09d, 0.06d, 0.90d, 0.85d);
    private static readonly SourceScoreProfile BlueprintScoreProfile = new(0d, 0.18d, 0.12d, 0.05d, 1.20d, 1.25d);

    public Task<IReadOnlyList<TaskResourceSuggestionDTO>> GetSuggestionsAsync(
        int taskId,
        int maxResults,
        bool includeResources = true,
        CancellationToken cancellationToken = default)
        => GetSuggestionsInternalAsync(taskId, maxResults, includeResources, sharedFeedbackStats: null, cancellationToken);

    private async Task<IReadOnlyList<TaskResourceSuggestionDTO>> GetSuggestionsInternalAsync(
        int taskId,
        int maxResults,
        bool includeResources,
        IReadOnlyDictionary<string, FeedbackStats>? sharedFeedbackStats,
        CancellationToken cancellationToken)
    {
        maxResults = Math.Clamp(maxResults, 1, 100);

        await using var tenantDb = await dbFactory.CreateDbContextAsync(cancellationToken);

        var target = await tenantDb.Tasks
            .AsNoTracking()
            .Where(x => x.Id == taskId && x.TenantId == tenantDb.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (target is null)
            return [];

        var targetContext = await BuildTenantTaskContextsAsync(tenantDb, [target], cancellationToken);
        var targetSuggestionContext = targetContext[target.Id];
        var targetNormalized = targetSuggestionContext.NormalizedText;

        if (string.IsNullOrWhiteSpace(targetNormalized))
            return [];

        var tenantId = tenantDb.TenantId;
        var feedbackBySuggestion = await GetFeedbackMapAsync(tenantId, target.Id, cancellationToken);
        // Use pre-fetched stats when available (bulk path) — avoids N redundant GROUP BY queries
        var feedbackStatsBySuggestion = sharedFeedbackStats
            ?? await GetFeedbackStatsMapAsync(tenantId, cancellationToken);

        var tenantTask = GetTenantTaskSuggestionsAsync(
            tenantDb,
            target.Id,
            targetSuggestionContext.ContextualName,
            targetNormalized,
            targetSuggestionContext.Unit,
            targetSuggestionContext.Code,
            targetSuggestionContext.CodeDepth,
            targetSuggestionContext.Quantity,
            maxResults,
            includeResources,
            mlRanker,
            tenantId,
            cancellationToken,
            targetParentNormalized: targetSuggestionContext.ParentNormalizedText,
            targetParentTaskId: targetSuggestionContext.ParentTaskId);

        var blueprintTask = GetBlueprintSuggestionsAsync(
            tenantId,
            targetSuggestionContext.ContextualName,
            targetNormalized,
            targetSuggestionContext.Unit,
            targetSuggestionContext.Code,
            targetSuggestionContext.CodeDepth,
            targetSuggestionContext.Quantity,
            maxResults,
            includeResources,
            mlRanker,
            cancellationToken,
            targetParentNormalized: targetSuggestionContext.ParentNormalizedText);

        await System.Threading.Tasks.Task.WhenAll(tenantTask, blueprintTask);

        return (await tenantTask)
            .Concat(await blueprintTask)
            .Select(x => ApplyFeedbackAdjustment(x, feedbackBySuggestion, feedbackStatsBySuggestion))
            .Where(x => !includeResources || x.Resources.Count > 0)
            .Where(x => x.Score >= MinimumScore)
            .OrderByDescending(GetPresentationScore)
            .ThenByDescending(x => x.Score)
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

    public async Task<bool> RecordFeedbackAsync(
        TaskResourceSuggestionFeedbackDTO feedback,
        CancellationToken cancellationToken = default)
    {
        await using var tenantDb = await dbFactory.CreateDbContextAsync(cancellationToken);
        var tenantId = tenantDb.TenantId;

        var target = await tenantDb.Tasks
            .AsNoTracking()
            .Where(x => x.Id == feedback.TargetTaskId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.Unit,
                x.Quantity
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (target is null || feedback.SourceTaskId <= 0)
            return false;

        await using var db = await blueprintFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.TaskResourceSuggestionFeedbacks
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.TargetTaskId == target.Id &&
                x.Source == feedback.Source &&
                x.SourceTaskId == feedback.SourceTaskId,
                cancellationToken);

        if (existing is null)
        {
            existing = new TaskResourceSuggestionFeedback
            {
                TenantId = tenantId,
                TargetTaskId = target.Id,
                Source = feedback.Source,
                SourceTaskId = feedback.SourceTaskId,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.TaskResourceSuggestionFeedbacks.Add(existing);
        }

        existing.TargetTaskName = target.Name;
        existing.TargetTaskCode = target.Code;
        existing.TargetTaskUnit = target.Unit;
        existing.TargetTaskQuantity = target.Quantity;
        existing.SourceTaskName = Limit(feedback.SourceTaskName, 256);
        existing.SourceTaskUnit = string.IsNullOrWhiteSpace(feedback.SourceTaskUnit) ? null : Limit(feedback.SourceTaskUnit, 64);
        existing.SourceTaskQuantity = feedback.SourceTaskQuantity;
        existing.Score = feedback.Score;
        existing.Feedback = feedback.Feedback;
        existing.ReviewStatus = TaskResourceSuggestionFeedbackReviewStatus.Pending;
        existing.ReviewedAtUtc = null;
        existing.Reason = string.IsNullOrWhiteSpace(feedback.Reason) ? null : Limit(feedback.Reason, 512);
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<List<TaskResourceSuggestionDTO>> GetTenantTaskSuggestionsAsync(
        ShardingSingleDbContext db,
        int targetTaskId,
        string targetName,
        string targetNormalized,
        string? targetUnit,
        string? targetCode,
        int targetCodeDepth,
        decimal? targetQuantity,
        int maxResults,
        bool includeResources,
        ITaskResourceSuggestionMlRanker mlRanker,
        int tenantId,
        CancellationToken ct,
        string targetParentNormalized = "",
        int? targetParentTaskId = null)
    {
        var baseQuery = db.Tasks
            .AsNoTracking()
            .Where(x => x.TenantId == db.TenantId && x.Id != targetTaskId && x.Resources.Any());

        var priorityQuery = ApplyTenantPriorityFilter(baseQuery, targetName, targetCode, targetUnit, targetNormalized)
            .OrderByDescending(x => x.Id)
            .Take(PriorityCandidateLimit);

        var fallbackQuery = ApplyTenantFallbackOrdering(baseQuery, targetCode, targetUnit, targetNormalized)
            .Take(FallbackCandidateLimit);

        if (includeResources)
        {
            priorityQuery = priorityQuery.Include(x => x.Resources);
            fallbackQuery = fallbackQuery.Include(x => x.Resources);
        }

        var priorityCandidates = await priorityQuery.ToListAsync(ct);
        var fallbackCandidates = await fallbackQuery.ToListAsync(ct);

        var candidates = MergeCandidates(priorityCandidates, fallbackCandidates, x => x.Id);

        var candidateContexts = await BuildTenantTaskContextsAsync(db, candidates, ct);
        // Pre-normalize once — reused across all candidates instead of per-candidate normalization
        var targetNameNormalized = SwedishTaskTextNormalizer.Normalize(targetName);

        // Load resource counts in one query (avoids N+1; Resources not included when includeResources=false)
        var candidateIds = candidates.Select(c => c.Id).ToList();
        var resourceCounts = includeResources
            ? candidates.ToDictionary(t => t.Id, t => t.Resources.Count)
            : await db.Tasks.AsNoTracking()
                .Where(x => candidateIds.Contains(x.Id))
                .Select(x => new { x.Id, Count = x.Resources.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        return candidates
            .Select(task =>
            {
                var candidateContext = candidateContexts[task.Id];
                var candidateNormalized = candidateContext.NormalizedText;

                var score = ApplyMlScore(
                    ExplainCandidateScoreForSource(
                    TaskResourceSuggestionSource.TenantTask,
                    targetNormalized,
                    candidateNormalized,
                    targetUnit,
                    candidateContext.Unit,
                    targetCode,
                    candidateContext.Code,
                    targetQuantity,
                    candidateContext.Quantity,
                    targetNameNormalized,
                    candidateContext.ContextualName,
                    targetCodeDepth,
                    candidateCodeDepth: candidateContext.CodeDepth,
                    targetParentNormalized: targetParentNormalized,
                    candidateParentNormalized: candidateContext.ParentNormalizedText,
                    targetParentTaskId: targetParentTaskId,
                    candidateParentTaskId: candidateContext.ParentTaskId)
                    with
                    {
                        ContextReason = BuildContextReason(targetCode, targetCodeDepth, candidateContext.Code, candidateContext.CodeDepth)
                    },
                    TaskResourceSuggestionSource.TenantTask,
                    mlRanker,
                    tenantId);

                return new TaskResourceSuggestionDTO
                {
                    SourceTaskId = task.Id,
                    SourceTaskName = task.Name,
                    SourceTaskCode = task.Code,
                    SourceTaskParentName = candidateContext.ParentName,
                    SourceTaskQuantity = candidateContext.Quantity,
                    SourceTaskUnit = candidateContext.Unit ?? string.Empty,
                    Source = TaskResourceSuggestionSource.TenantTask,
                    Score = score.Score,
                    Reason = BuildReason(score, TaskResourceSuggestionSource.TenantTask),
                    ScoreDetails = BuildScoreDetails(score, TaskResourceSuggestionSource.TenantTask),
                    ResourceCount = resourceCounts.GetValueOrDefault(task.Id),
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
        int targetCodeDepth,
        decimal? targetQuantity,
        int maxResults,
        bool includeResources,
        ITaskResourceSuggestionMlRanker mlRanker,
        CancellationToken ct,
        string targetParentNormalized = "")
    {
        await using var db = await blueprintFactory.CreateDbContextAsync(ct);

        var suggestionStatuses = new[] { TaskStatusEnum.Ready, TaskStatusEnum.SuggestionOnly };
        var baseQuery = db.Tasks
            .AsNoTracking()
            .Where(x =>
                suggestionStatuses.Contains(x.Status) &&
                x.ResourceLinks.Any(link =>
                    link.Resource != null &&
                    link.Resource.IsActive &&
                    link.Resource.IsVisible));

        var priorityQuery = ApplyBlueprintPriorityFilter(baseQuery, targetName, targetCode, targetUnit, targetNormalized)
            .OrderBy(x => x.SortOrder)
            .Take(PriorityCandidateLimit);

        var fallbackQuery = ApplyBlueprintFallbackOrdering(baseQuery, targetCode, targetUnit, targetNormalized)
            .Take(FallbackCandidateLimit);

        var priorityCandidates = await priorityQuery.ToListAsync(ct);
        var fallbackCandidates = await fallbackQuery.ToListAsync(ct);

        var candidates = MergeCandidates(priorityCandidates, fallbackCandidates, x => x.Id);
        // Pre-normalize once — reused across all candidates instead of per-candidate normalization
        var targetNameNormalized = SwedishTaskTextNormalizer.Normalize(targetName);

        return candidates
            .Select(task =>
            {
                var candidateContext = BuildBlueprintTaskContext(task);

                var score = ApplyMlScore(
                    ExplainCandidateScoreForSource(
                    TaskResourceSuggestionSource.BlueprintTask,
                    targetNormalized,
                    candidateContext.NormalizedText,
                    targetUnit,
                    candidateContext.Unit,
                    targetCode,
                    candidateContext.Code,
                    targetQuantity,
                    candidateContext.Quantity,
                    targetNameNormalized,
                    candidateContext.ContextualName,
                    targetCodeDepth,
                    candidateCodeDepth: candidateContext.CodeDepth,
                    targetParentNormalized: targetParentNormalized,
                    candidateParentNormalized: candidateContext.ParentNormalizedText)
                    with
                    {
                        ContextReason = BuildContextReason(targetCode, targetCodeDepth, candidateContext.Code, candidateContext.CodeDepth)
                    },
                    TaskResourceSuggestionSource.BlueprintTask,
                    mlRanker,
                    tenantId);

                return new TaskResourceSuggestionDTO
                {
                    SourceTaskId = task.Id,
                    SourceTaskName = task.Name,
                    SourceTaskCode = task.Code,
                    SourceTaskParentName = candidateContext.ParentName,
                    SourceTaskQuantity = null,
                    SourceTaskUnit = candidateContext.Unit ?? string.Empty,
                    Source = TaskResourceSuggestionSource.BlueprintTask,
                    Score = score.Score,
                    Reason = BuildReason(score, TaskResourceSuggestionSource.BlueprintTask),
                    ScoreDetails = BuildScoreDetails(score, TaskResourceSuggestionSource.BlueprintTask),
                    ResourceCount = task.ResourceLinks.Count(l => l.Resource is { IsActive: true, IsVisible: true }),
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
        string? targetUnit,
        string targetNormalized)
    {
        var hasName = !string.IsNullOrWhiteSpace(targetName);
        var hasCode = !string.IsNullOrWhiteSpace(targetCode);
        var hasUnit = !string.IsNullOrWhiteSpace(targetUnit);
        var tokens = ExtractPriorityTokens(targetNormalized);
        var hasStrongMatch = hasName || hasCode;

        if (!hasName && !hasCode && !hasUnit && tokens.Count == 0)
            return query.Where(_ => false);

        return tokens.Count switch
        {
            0 => query.Where(x =>
                (hasName && x.Name == targetName) ||
                (hasCode && x.Code != null && x.Code == targetCode) ||
                (hasCode && x.ParentTask != null && x.ParentTask.Code != null && x.ParentTask.Code == targetCode) ||
                (!hasStrongMatch && hasUnit && x.Unit != null && x.Unit == targetUnit)),
            1 => query.Where(x =>
                (hasName && x.Name == targetName) ||
                (hasCode && x.Code != null && x.Code == targetCode) ||
                (hasCode && x.ParentTask != null && x.ParentTask.Code != null && x.ParentTask.Code == targetCode) ||
                (!hasStrongMatch && hasUnit && x.Unit != null && x.Unit == targetUnit) ||
                x.NormalizedTextSv.Contains(tokens[0]) ||
                (x.ParentTask != null && x.ParentTask.NormalizedTextSv.Contains(tokens[0]))),
            2 => query.Where(x =>
                (hasName && x.Name == targetName) ||
                (hasCode && x.Code != null && x.Code == targetCode) ||
                (hasCode && x.ParentTask != null && x.ParentTask.Code != null && x.ParentTask.Code == targetCode) ||
                (!hasStrongMatch && hasUnit && x.Unit != null && x.Unit == targetUnit) ||
                x.NormalizedTextSv.Contains(tokens[0]) ||
                x.NormalizedTextSv.Contains(tokens[1]) ||
                (x.ParentTask != null && (x.ParentTask.NormalizedTextSv.Contains(tokens[0]) ||
                                          x.ParentTask.NormalizedTextSv.Contains(tokens[1])))),
            _ => query.Where(x =>
                (hasName && x.Name == targetName) ||
                (hasCode && x.Code != null && x.Code == targetCode) ||
                (hasCode && x.ParentTask != null && x.ParentTask.Code != null && x.ParentTask.Code == targetCode) ||
                (!hasStrongMatch && hasUnit && x.Unit != null && x.Unit == targetUnit) ||
                x.NormalizedTextSv.Contains(tokens[0]) ||
                x.NormalizedTextSv.Contains(tokens[1]) ||
                x.NormalizedTextSv.Contains(tokens[2]) ||
                (x.ParentTask != null && (x.ParentTask.NormalizedTextSv.Contains(tokens[0]) ||
                                          x.ParentTask.NormalizedTextSv.Contains(tokens[1]) ||
                                          x.ParentTask.NormalizedTextSv.Contains(tokens[2]))))
        };
    }

    private static IOrderedQueryable<TaskEntity> ApplyTenantFallbackOrdering(
        IQueryable<TaskEntity> query,
        string? targetCode,
        string? targetUnit,
        string targetNormalized)
    {
        var hasCode = !string.IsNullOrWhiteSpace(targetCode);
        var hasUnit = !string.IsNullOrWhiteSpace(targetUnit);
        var tokens = ExtractPriorityTokens(targetNormalized);
        var firstToken = tokens.Count > 0 ? tokens[0] : string.Empty;
        var secondToken = tokens.Count > 1 ? tokens[1] : string.Empty;

        return query
            .OrderByDescending(x => hasCode && (
                (x.Code != null && x.Code == targetCode) ||
                (x.ParentTask != null && x.ParentTask.Code != null && x.ParentTask.Code == targetCode)))
            .ThenByDescending(x => hasUnit && x.Unit != null && x.Unit == targetUnit)
            .ThenByDescending(x => firstToken != string.Empty && (
                x.NormalizedTextSv.Contains(firstToken) ||
                (x.ParentTask != null && x.ParentTask.NormalizedTextSv.Contains(firstToken))))
            .ThenByDescending(x => secondToken != string.Empty && (
                x.NormalizedTextSv.Contains(secondToken) ||
                (x.ParentTask != null && x.ParentTask.NormalizedTextSv.Contains(secondToken))))
            .ThenByDescending(x => x.Id);
    }

    private static async Task<Dictionary<int, TenantTaskSuggestionContext>> BuildTenantTaskContextsAsync(
        ShardingSingleDbContext db,
        IReadOnlyList<TaskEntity> tasks,
        CancellationToken ct)
    {
        var ancestorById = new Dictionary<int, TenantTaskAncestorContext>();
        var pendingParentIds = tasks
            .Select(x => x.ParentTaskId)
            .OfType<int>()
            .Distinct()
            .ToHashSet();

        for (var depth = 0; depth < 8 && pendingParentIds.Count > 0; depth++)
        {
            var currentParentIds = pendingParentIds.ToList();
            pendingParentIds.Clear();

            var parents = await db.Tasks
                .AsNoTracking()
                .Where(x => currentParentIds.Contains(x.Id))
                .Select(x => new TenantTaskAncestorContext(
                    x.Id,
                    x.ParentTaskId,
                    x.Name,
                    x.Code))
                .ToListAsync(ct);

            foreach (var parent in parents)
            {
                if (!ancestorById.TryAdd(parent.Id, parent))
                    continue;

                if (parent.ParentTaskId is { } parentTaskId && !ancestorById.ContainsKey(parentTaskId))
                    pendingParentIds.Add(parentTaskId);
            }
        }

        return tasks.ToDictionary(
            x => x.Id,
            x =>
            {
                var ancestors = GetAncestors(x.ParentTaskId, ancestorById);
                return BuildTenantTaskContext(x, ancestors);
            });
    }

    private static List<TenantTaskAncestorContext> GetAncestors(
        int? parentTaskId,
        IReadOnlyDictionary<int, TenantTaskAncestorContext> ancestorById)
    {
        var ancestors = new List<TenantTaskAncestorContext>();
        var seen = new HashSet<int>();
        var currentParentId = parentTaskId;

        while (currentParentId is { } id && seen.Add(id) && ancestorById.TryGetValue(id, out var parent))
        {
            ancestors.Add(parent);
            currentParentId = parent.ParentTaskId;
        }

        ancestors.Reverse();
        return ancestors;
    }

    private static TenantTaskSuggestionContext BuildTenantTaskContext(
        TaskEntity task,
        IReadOnlyList<TenantTaskAncestorContext> ancestors)
    {
        var ancestorCode = ancestors
            .Select((ancestor, index) => new
            {
                ancestor.Code,
                Depth = ancestors.Count - index
            })
            .LastOrDefault(x => !string.IsNullOrWhiteSpace(x.Code));
        var effectiveCode = string.IsNullOrWhiteSpace(task.Code) ? ancestorCode?.Code : task.Code;
        var codeDepth = string.IsNullOrWhiteSpace(task.Code) && !string.IsNullOrWhiteSpace(effectiveCode)
            ? ancestorCode?.Depth ?? 1
            : 0;
        var rawTextParts = new List<string>();
        var parentTextParts = new List<string>();

        foreach (var ancestor in ancestors)
        {
            rawTextParts.Add(ancestor.Code ?? string.Empty);
            rawTextParts.Add(ancestor.Name);
            if (!string.IsNullOrWhiteSpace(ancestor.Name))
                parentTextParts.Add(ancestor.Name);
        }

        rawTextParts.Add(effectiveCode ?? string.Empty);
        rawTextParts.Add(task.Name);

        // Unit is excluded from semantic text so compatible units don't penalize Jaccard.
        // Unit scoring is handled separately in ExplainCandidateScore.
        var normalizedText = SwedishTaskTextNormalizer.Normalize(string.Join(' ', rawTextParts));
        var parentNormalizedText = parentTextParts.Count > 0
            ? SwedishTaskTextNormalizer.Normalize(string.Join(' ', parentTextParts))
            : string.Empty;
        var contextualName = BuildContextualName(ancestors.Select(x => x.Name), task.Name);

        var immediateParentName   = ancestors.Count > 0 ? ancestors[^1].Name : null;
        var immediateParentTaskId = task.ParentTaskId;

        return new TenantTaskSuggestionContext(
            task.Id,
            task.Name,
            contextualName,
            normalizedText,
            parentNormalizedText,
            immediateParentName,
            immediateParentTaskId,
            task.Unit,
            effectiveCode,
            codeDepth,
            task.Quantity);
    }

    private static BlueprintTaskSuggestionContext BuildBlueprintTaskContext(TaskDefinition task)
    {
        var effectiveCode = string.IsNullOrWhiteSpace(task.Code) ? task.ParentCode : task.Code;
        var codeDepth = string.IsNullOrWhiteSpace(task.Code) && !string.IsNullOrWhiteSpace(task.ParentCode)
            ? 1
            : 0;
        var normalizedText = string.IsNullOrWhiteSpace(task.NormalizedTextSv)
            ? SwedishTaskTextNormalizer.NormalizeTask(task.Name, effectiveCode, null)
            : task.NormalizedTextSv;

        // Parent-only hierarchy names (exclude the last segment which is the task itself)
        var hierarchyNames = SplitHierarchyNames(task.HierarchyPath, task.Name)
            .OfType<string>()
            .Where(n => !string.Equals(n.Trim(), task.Name.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
        var parentNormalizedText = hierarchyNames.Count > 0
            ? SwedishTaskTextNormalizer.Normalize(string.Join(' ', hierarchyNames))
            : string.Empty;

        var contextualName = BuildContextualName(
            SplitHierarchyNames(task.HierarchyPath, task.Name),
            task.Name);

        var blueprintParentName = hierarchyNames.Count > 0 ? hierarchyNames[^1] : null;

        return new BlueprintTaskSuggestionContext(
            task.Name,
            contextualName,
            normalizedText,
            parentNormalizedText,
            blueprintParentName,
            task.UnitCode,
            effectiveCode,
            codeDepth,
            task.Quantity);
    }

    private static IQueryable<TaskDefinition> ApplyBlueprintPriorityFilter(
        IQueryable<TaskDefinition> query,
        string targetName,
        string? targetCode,
        string? targetUnit,
        string targetNormalized)
    {
        var hasName = !string.IsNullOrWhiteSpace(targetName);
        var hasCode = !string.IsNullOrWhiteSpace(targetCode);
        var hasUnit = !string.IsNullOrWhiteSpace(targetUnit);
        var tokens = ExtractPriorityTokens(targetNormalized);
        var hasStrongMatch = hasName || hasCode;

        if (!hasName && !hasCode && !hasUnit && tokens.Count == 0)
            return query.Where(_ => false);

        return tokens.Count switch
        {
            0 => query.Where(x =>
                (hasName && x.Name == targetName) ||
                (hasCode && x.Code != null && x.Code == targetCode) ||
                (!hasStrongMatch && hasUnit && x.UnitCode != null && x.UnitCode == targetUnit)),
            1 => query.Where(x =>
                (hasName && x.Name == targetName) ||
                (hasCode && x.Code != null && x.Code == targetCode) ||
                (!hasStrongMatch && hasUnit && x.UnitCode != null && x.UnitCode == targetUnit) ||
                x.NormalizedTextSv.Contains(tokens[0])),
            2 => query.Where(x =>
                (hasName && x.Name == targetName) ||
                (hasCode && x.Code != null && x.Code == targetCode) ||
                (!hasStrongMatch && hasUnit && x.UnitCode != null && x.UnitCode == targetUnit) ||
                x.NormalizedTextSv.Contains(tokens[0]) ||
                x.NormalizedTextSv.Contains(tokens[1])),
            _ => query.Where(x =>
                (hasName && x.Name == targetName) ||
                (hasCode && x.Code != null && x.Code == targetCode) ||
                (!hasStrongMatch && hasUnit && x.UnitCode != null && x.UnitCode == targetUnit) ||
                x.NormalizedTextSv.Contains(tokens[0]) ||
                x.NormalizedTextSv.Contains(tokens[1]) ||
                x.NormalizedTextSv.Contains(tokens[2]))
        };
    }

    private static IOrderedQueryable<TaskDefinition> ApplyBlueprintFallbackOrdering(
        IQueryable<TaskDefinition> query,
        string? targetCode,
        string? targetUnit,
        string targetNormalized)
    {
        var hasCode = !string.IsNullOrWhiteSpace(targetCode);
        var hasUnit = !string.IsNullOrWhiteSpace(targetUnit);
        var tokens = ExtractPriorityTokens(targetNormalized);
        var firstToken = tokens.Count > 0 ? tokens[0] : string.Empty;
        var secondToken = tokens.Count > 1 ? tokens[1] : string.Empty;

        return query
            .OrderByDescending(x => hasCode && (
                (x.Code != null && x.Code == targetCode) ||
                (x.ParentCode != null && x.ParentCode == targetCode)))
            .ThenByDescending(x => hasUnit && x.UnitCode != null && x.UnitCode == targetUnit)
            .ThenByDescending(x => firstToken != string.Empty && x.NormalizedTextSv.Contains(firstToken))
            .ThenByDescending(x => secondToken != string.Empty && x.NormalizedTextSv.Contains(secondToken))
            .ThenBy(x => x.SortOrder);
    }

    private static List<string> ExtractPriorityTokens(string normalizedText)
        => normalizedText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length >= 3 && !x.Contains('_'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Any(char.IsDigit))
            .ThenByDescending(x => x.Length)
            .Take(3)
            .ToList();

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
            .Where(x => x.Id == sourceTaskId && x.TenantId == db.TenantId)
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

        var links = await db.TaskDefinitionResourceLinks
            .AsNoTracking()
            .Where(x => x.TaskDefinitionId == sourceTaskId)
            .Include(x => x.Resource)
                .ThenInclude(r => r!.TenantLinks.Where(t => t.TenantId == tenantId))
            .ToListAsync(ct);

        if (links.Count == 0)
            return [];

        return links
            .Where(l => l.Resource is { IsActive: true })
            .OrderBy(l => l.Resource!.SortOrder)
            .Select(link =>
            {
                var res = link.Resource!;
                var tenantLink = res.TenantLinks.FirstOrDefault();
                var data = res.Data.Clone();

                data.Parameters = link.Parameters;
                data.AddOns = link.AddOns;
                data.Times = link.Times;

                if (tenantLink?.Cost is not null)
                    data.Cost = tenantLink.Cost.Value;

                return new ResourcePostDTO
                {
                    Name = tenantLink?.Name is { Length: > 0 } n ? n : res.Name,
                    ResType = res.ResType,
                    IsActive = res.IsActive,
                    StatusId = tenantLink?.StatusId,
                    ResourceTypeId = tenantLink?.ResourceTypeId,
                    ResourceSortId = tenantLink?.ResourceSortId,
                    AccountId = tenantLink?.AccountId,
                    SortOrder = res.SortOrder,
                    Quantity = link.Quantity,
                    Data = data,
                    CostRole = res.CostRoles,
                };
            })
            .ToList();
    }

    private async Task<Dictionary<string, TaskResourceSuggestionFeedback>> GetFeedbackMapAsync(
        int tenantId,
        int targetTaskId,
        CancellationToken ct)
    {
        await using var db = await blueprintFactory.CreateDbContextAsync(ct);
        var feedbacks = await db.TaskResourceSuggestionFeedbacks
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TargetTaskId == targetTaskId)
            .ToListAsync(ct);

        return feedbacks.ToDictionary(x => FeedbackKey(x.Source, x.SourceTaskId));
    }

    private static TaskResourceSuggestionDTO ApplyFeedbackAdjustment(
        TaskResourceSuggestionDTO suggestion,
        IReadOnlyDictionary<string, TaskResourceSuggestionFeedback> feedbackBySuggestion,
        IReadOnlyDictionary<string, FeedbackStats> feedbackStatsBySuggestion)
    {
        if (!feedbackBySuggestion.TryGetValue(FeedbackKey(suggestion.Source, suggestion.SourceTaskId), out var feedback))
        {
            ApplyGlobalFeedbackAdjustment(suggestion, feedbackStatsBySuggestion);
            return suggestion;
        }

        suggestion.Feedback = feedback.Feedback;
        suggestion.Score = ApplyDirectFeedbackScore(suggestion.Score, suggestion.Source, feedback.Feedback);
        suggestion.Reason = $"{suggestion.Reason} Feedback: {feedback.Feedback}.";
        suggestion.ScoreDetails.Add(new TaskResourceSuggestionScoreDetailDTO
        {
            Label = feedback.Feedback.ToString(),
            Kind = "feedback"
        });
        return suggestion;
    }

    private static void ApplyGlobalFeedbackAdjustment(
        TaskResourceSuggestionDTO suggestion,
        IReadOnlyDictionary<string, FeedbackStats> feedbackStatsBySuggestion)
    {
        if (!feedbackStatsBySuggestion.TryGetValue(FeedbackKey(suggestion.Source, suggestion.SourceTaskId), out var stats) ||
            stats.Total == 0)
            return;

        var acceptedWeight = suggestion.Source == TaskResourceSuggestionSource.TenantTask ? 0.020d : 0.015d;
        var positiveCap = suggestion.Source == TaskResourceSuggestionSource.TenantTask ? 0.08d : 0.06d;
        var positiveLift = Math.Min(positiveCap, stats.Accepted * acceptedWeight);

        // Ratio-confidence boost: when most reviewers accepted a suggestion, it's reliably good.
        // Requires ≥3 total votes to avoid noise from single-vote samples.
        if (stats.Total >= 3)
        {
            var acceptanceRatio = (double)stats.Accepted / stats.Total;
            if (acceptanceRatio >= 0.70d)
            {
                var ratioCap = suggestion.Source == TaskResourceSuggestionSource.TenantTask ? 0.12d : 0.09d;
                // Extra lift scales with how far above 70% the ratio is (max +3% at 100%)
                var ratioLift = (acceptanceRatio - 0.70d) * 0.10d;
                positiveLift = Math.Min(ratioCap, positiveLift + ratioLift);
            }
        }

        var negativePressure =
            Math.Min(0.18d,
                (stats.Rejected * 0.05d) +
                (stats.WrongUnit * 0.055d) +
                (stats.WrongResourceType * 0.050d) +
                (stats.MissingResources * 0.010d));

        var adjustment = positiveLift - negativePressure;
        if (Math.Abs(adjustment) < 0.005d)
            return;

        suggestion.Score = Math.Round(Math.Clamp(suggestion.Score + adjustment, 0d, 1d), 4);
        suggestion.Reason = $"{suggestion.Reason} Global feedback {FormatSignedPercent(adjustment)}.";
        suggestion.ScoreDetails.Add(new TaskResourceSuggestionScoreDetailDTO
        {
            Label = $"Feedback {FormatSignedPercent(adjustment)}",
            Value = adjustment,
            Kind = adjustment >= 0 ? "feedback" : "warning"
        });
    }

    private static double ApplyDirectFeedbackScore(
        double score,
        TaskResourceSuggestionSource source,
        TaskResourceSuggestionFeedbackKind feedback)
    {
        var acceptedLift = source == TaskResourceSuggestionSource.TenantTask ? 0.10d : 0.08d;
        return feedback switch
        {
            TaskResourceSuggestionFeedbackKind.Accepted => Math.Round(Math.Min(1d, score + acceptedLift), 4),
            TaskResourceSuggestionFeedbackKind.Rejected => Math.Round(score * 0.35d, 4),
            TaskResourceSuggestionFeedbackKind.WrongUnit => Math.Round(score * 0.45d, 4),
            TaskResourceSuggestionFeedbackKind.WrongResourceType => Math.Round(score * 0.50d, 4),
            TaskResourceSuggestionFeedbackKind.MissingResources => Math.Round(score * (source == TaskResourceSuggestionSource.BlueprintTask ? 0.70d : 0.85d), 4),
            _ => score
        };
    }

    private static string FeedbackKey(TaskResourceSuggestionSource source, int sourceTaskId)
        => $"{source}:{sourceTaskId}";

    private static double GetPresentationScore(TaskResourceSuggestionDTO suggestion)
    {
        var lift = 0d;
        if (suggestion.Source == TaskResourceSuggestionSource.TenantTask)
        {
            var quantity = suggestion.ScoreDetails.FirstOrDefault(x => x.Kind == "quantity")?.Value;
            if (quantity is >= 0.80d)
                lift += 0.025d;
        }
        else if (suggestion.Source == TaskResourceSuggestionSource.BlueprintTask)
        {
            var code = suggestion.ScoreDetails.FirstOrDefault(x => x.Kind == "code")?.Value;
            if (code is >= 0.10d)
                lift += 0.015d;
        }

        return Math.Min(1d, suggestion.Score + lift);
    }

    private async Task<Dictionary<string, FeedbackStats>> GetFeedbackStatsMapAsync(
        int tenantId,
        CancellationToken ct)
    {
        await using var db = await blueprintFactory.CreateDbContextAsync(ct);
        var rows = await db.TaskResourceSuggestionFeedbacks
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.ReviewStatus == TaskResourceSuggestionFeedbackReviewStatus.Approved)
            .GroupBy(x => new { x.Source, x.SourceTaskId })
            .Select(x => new
            {
                x.Key.Source,
                x.Key.SourceTaskId,
                Accepted = x.Count(f => f.Feedback == TaskResourceSuggestionFeedbackKind.Accepted),
                Rejected = x.Count(f => f.Feedback == TaskResourceSuggestionFeedbackKind.Rejected),
                WrongUnit = x.Count(f => f.Feedback == TaskResourceSuggestionFeedbackKind.WrongUnit),
                WrongResourceType = x.Count(f => f.Feedback == TaskResourceSuggestionFeedbackKind.WrongResourceType),
                MissingResources = x.Count(f => f.Feedback == TaskResourceSuggestionFeedbackKind.MissingResources)
            })
            .ToListAsync(ct);

        return rows.ToDictionary(
            x => FeedbackKey(x.Source, x.SourceTaskId),
            x => new FeedbackStats(
                x.Accepted,
                x.Rejected,
                x.WrongUnit,
                x.WrongResourceType,
                x.MissingResources));
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
        => ExplainCandidateScore(
            targetNormalized,
            candidateNormalized,
            targetUnit,
            candidateUnit,
            targetCode,
            candidateCode,
            targetQuantity,
            candidateQuantity,
            targetName,
            candidateName).Score;

    private static CandidateScoreBreakdown ExplainCandidateScore(
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
        => ExplainCandidateScoreCore(
            targetNormalized,
            candidateNormalized,
            targetUnit,
            candidateUnit,
            targetCode,
            candidateCode,
            targetQuantity,
            candidateQuantity,
            SwedishTaskTextNormalizer.Normalize(targetName),
            candidateName,
            codeInheritanceFactor: 1d,
            hasInheritedContext: false,
            DefaultScoreProfile);

    private static CandidateScoreBreakdown ExplainCandidateScoreWithCodeContext(
        string targetNormalized,
        string candidateNormalized,
        string? targetUnit,
        string? candidateUnit,
        string? targetCode = null,
        string? candidateCode = null,
        decimal? targetQuantity = null,
        decimal? candidateQuantity = null,
        string targetNameNormalized = "",
        string? candidateName = null,
        int targetCodeDepth = 0,
        int candidateCodeDepth = 0)
        => ExplainCandidateScoreCore(
            targetNormalized,
            candidateNormalized,
            targetUnit,
            candidateUnit,
            targetCode,
            candidateCode,
            targetQuantity,
            candidateQuantity,
            targetNameNormalized,
            candidateName,
            GetCodeInheritanceFactor(targetCodeDepth, candidateCodeDepth),
            targetCodeDepth > 0 || candidateCodeDepth > 0,
            DefaultScoreProfile);

    private static CandidateScoreBreakdown ExplainCandidateScoreForSource(
        TaskResourceSuggestionSource source,
        string targetNormalized,
        string candidateNormalized,
        string? targetUnit,
        string? candidateUnit,
        string? targetCode = null,
        string? candidateCode = null,
        decimal? targetQuantity = null,
        decimal? candidateQuantity = null,
        string targetNameNormalized = "",
        string? candidateName = null,
        int targetCodeDepth = 0,
        int candidateCodeDepth = 0,
        string targetParentNormalized = "",
        string candidateParentNormalized = "",
        int? targetParentTaskId = null,
        int? candidateParentTaskId = null)
    {
        if (source == TaskResourceSuggestionSource.BlueprintTask)
        {
            targetQuantity = null;
            candidateQuantity = null;
        }

        return ExplainCandidateScoreCore(
            targetNormalized,
            candidateNormalized,
            targetUnit,
            candidateUnit,
            targetCode,
            candidateCode,
            targetQuantity,
            candidateQuantity,
            targetNameNormalized,
            candidateName,
            GetCodeInheritanceFactor(targetCodeDepth, candidateCodeDepth),
            targetCodeDepth > 0 || candidateCodeDepth > 0,
            GetScoreProfile(source),
            targetParentNormalized,
            candidateParentNormalized,
            targetParentTaskId,
            candidateParentTaskId);
    }

    private static CandidateScoreBreakdown ExplainCandidateScoreCore(
        string targetNormalized,
        string candidateNormalized,
        string? targetUnit,
        string? candidateUnit,
        string? targetCode,
        string? candidateCode,
        decimal? targetQuantity,
        decimal? candidateQuantity,
        string targetNameNormalized,
        string? candidateName,
        double codeInheritanceFactor,
        bool hasInheritedContext,
        SourceScoreProfile profile,
        string targetParentNormalized = "",
        string candidateParentNormalized = "",
        int? targetParentTaskId = null,
        int? candidateParentTaskId = null)
    {
        var textScore = SwedishTaskTextNormalizer.CalculateSimilarity(targetNormalized, candidateNormalized);
        var nameScore = GetNameSimilarity(targetNameNormalized, candidateName);
        var tokenCoverage = SwedishTaskTextNormalizer.CalculateTokenCoverage(targetNormalized, candidateNormalized);
        var reverseTokenCoverage = SwedishTaskTextNormalizer.CalculateTokenCoverage(candidateNormalized, targetNormalized);
        var hybridTokenScore = (tokenCoverage * 0.75d) + (reverseTokenCoverage * 0.25d);
        var semanticScore = Math.Max(
            textScore,
            (textScore * 0.45d) + (nameScore * 0.25d) + (hybridTokenScore * 0.30d));
        var score = semanticScore;
        var quantitySimilarity = GetQuantitySimilarity(
            targetQuantity,
            targetUnit,
            candidateQuantity,
            candidateUnit);

        score = ApplyQuantityScore(score, quantitySimilarity, profile.QuantityWeight);

        var hasEquivalentUnits = QuantityUnitNormalizer.AreEquivalentUnits(targetUnit, candidateUnit);
        var hasCompatibleUnits = QuantityUnitNormalizer.AreCompatibleUnits(targetUnit, candidateUnit);
        var unitBonus = 0d;
        if (!string.IsNullOrWhiteSpace(targetUnit) && !string.IsNullOrWhiteSpace(candidateUnit))
        {
            if (hasEquivalentUnits)
                unitBonus = profile.EquivalentUnitBonus;
            else if (hasCompatibleUnits)
                unitBonus = profile.CompatibleUnitBonus;
            else
                unitBonus = -profile.IncompatibleUnitPenalty;
        }

        score = Math.Clamp(score + unitBonus, 0d, 1d);

        var codeBonus = SwedishTaskTextNormalizer.GetCodeHierarchyScore(targetCode, candidateCode) * codeInheritanceFactor * profile.CodeWeightMultiplier;
        score = Math.Min(1d, score + codeBonus);

        var parentContextBonus = 0d;
        if (hasInheritedContext &&
            codeBonus > 0d &&
            hybridTokenScore >= 0.25d)
        {
            parentContextBonus = ParentContextBonus * codeInheritanceFactor * profile.ParentContextMultiplier;
            score = Math.Min(1d, score + parentContextBonus);
        }

        // Parent name bonus: boost when both tasks share similar parent context.
        // Stronger when the task name itself is short/generic (few tokens → less discriminating).
        var parentNameBonus = 0d;
        if (!string.IsNullOrWhiteSpace(targetParentNormalized) &&
            !string.IsNullOrWhiteSpace(candidateParentNormalized))
        {
            var parentSimilarity = SwedishTaskTextNormalizer.CalculateSimilarity(
                targetParentNormalized, candidateParentNormalized);
            if (parentSimilarity >= 0.30d)
            {
                var taskNameTokenCount = targetNameNormalized
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                var genericityFactor = taskNameTokenCount <= 1 ? 1.0d
                    : taskNameTokenCount <= 2 ? 0.70d
                    : taskNameTokenCount <= 3 ? 0.45d
                    : 0.25d;
                parentNameBonus = parentSimilarity * 0.12d * genericityFactor * profile.ParentContextMultiplier;
                score = Math.Min(1d, score + parentNameBonus);
            }
        }

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
        else if (quantitySimilarity.HasValue)
        {
            score = Math.Min(score, 0.955d);
        }

        // Sibling boost: tasks sharing the same immediate parent are structurally related.
        var siblingBonus = 0d;
        if (targetParentTaskId.HasValue &&
            candidateParentTaskId.HasValue &&
            targetParentTaskId.Value == candidateParentTaskId.Value)
        {
            siblingBonus = 0.05d * profile.ParentContextMultiplier;
            score = Math.Min(1d, score + siblingBonus);
        }

        return new CandidateScoreBreakdown(
            Score: Math.Round(score, 4),
            TextScore: Math.Round(textScore, 4),
            NameScore: Math.Round(nameScore, 4),
            TokenCoverage: Math.Round(tokenCoverage, 4),
            HybridTokenScore: Math.Round(hybridTokenScore, 4),
            QuantitySimilarity: quantitySimilarity.HasValue ? Math.Round(quantitySimilarity.Value, 4) : null,
            UnitBonus: unitBonus,
            CodeBonus: codeBonus,
            ParentContextBonus: Math.Round(parentContextBonus, 4),
            ParentNameBonus: Math.Round(parentNameBonus, 4),
            SiblingBonus: Math.Round(siblingBonus, 4),
            HasEquivalentUnits: hasEquivalentUnits,
            HasCompatibleUnits: hasCompatibleUnits);
    }

    private static double GetCodeInheritanceFactor(int targetCodeDepth, int candidateCodeDepth)
    {
        var maxDepth = Math.Max(Math.Max(targetCodeDepth, candidateCodeDepth), 0);
        return maxDepth switch
        {
            0 => 1d,
            1 => 0.75d,
            _ => 0.55d
        };
    }

    private static string? BuildContextReason(
        string? targetCode,
        int targetCodeDepth,
        string? candidateCode,
        int candidateCodeDepth)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(targetCode) && targetCodeDepth > 0)
            parts.Add($"target uses {FormatInheritedCodeDepth(targetCodeDepth)} code {targetCode}");

        if (!string.IsNullOrWhiteSpace(candidateCode) && candidateCodeDepth > 0)
            parts.Add($"candidate uses {FormatInheritedCodeDepth(candidateCodeDepth)} code {candidateCode}");

        return parts.Count == 0 ? null : string.Join("; ", parts);
    }

    private static string BuildContextualName(IEnumerable<string?> parentNames, string taskName)
    {
        var names = parentNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Where(x => !string.Equals(x, taskName, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        names.Add(taskName.Trim());
        return string.Join(' ', names);
    }

    private static IEnumerable<string?> SplitHierarchyNames(string? hierarchyPath, string taskName)
    {
        if (string.IsNullOrWhiteSpace(hierarchyPath))
        {
            if (!string.IsNullOrWhiteSpace(taskName))
                yield return taskName;
            yield break;
        }

        foreach (var part in hierarchyPath.Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var trimmed = part.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            var nameOnly = trimmed;
            var firstSpace = trimmed.IndexOf(' ');
            if (firstSpace > 0 && trimmed[..firstSpace].Any(char.IsLetterOrDigit))
                nameOnly = trimmed[(firstSpace + 1)..].Trim();

            if (!string.IsNullOrWhiteSpace(nameOnly))
                yield return nameOnly;
        }
    }

    private static string FormatInheritedCodeDepth(int depth)
        => depth <= 1 ? "parent" : "ancestor";

    private static CandidateScoreBreakdown ApplyMlScore(
        CandidateScoreBreakdown score,
        TaskResourceSuggestionSource source,
        ITaskResourceSuggestionMlRanker mlRanker,
        int tenantId)
    {
        var mlScore = mlRanker.PredictScore(new TaskResourceSuggestionMlFeatures(
            HeuristicScore: score.Score,
            TextScore: score.TextScore,
            NameScore: score.NameScore,
            QuantitySimilarity: score.QuantitySimilarity ?? 0d,
            HasQuantitySimilarity: score.QuantitySimilarity.HasValue,
            UnitBonus: score.UnitBonus,
            CodeBonus: score.CodeBonus,
            HasEquivalentUnits: score.HasEquivalentUnits,
            HasCompatibleUnits: score.HasCompatibleUnits,
            IsBlueprintSource: source == TaskResourceSuggestionSource.BlueprintTask),
            tenantId);

        if (!mlScore.HasValue)
            return score;

        var blendWeight = score.TextScore < 0.35d && score.NameScore < 0.35d
            ? 0.15d
            : 0.25d;
        var blendedScore = Math.Round(
            Math.Clamp((score.Score * (1d - blendWeight)) + (mlScore.Value * blendWeight), 0d, 1d),
            4);

        return score with
        {
            Score = blendedScore,
            MlScore = Math.Round(mlScore.Value, 4),
            MlBlendWeight = blendWeight
        };
    }

    private static double GetNameSimilarity(string targetNameNormalized, string? candidateName)
    {
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

    private static double ApplyQuantityScore(double currentScore, double? quantitySimilarity, double quantityWeight)
    {
        if (quantitySimilarity is null || quantityWeight <= 0d)
            return currentScore;

        return Math.Clamp((currentScore * (1d - quantityWeight)) + (quantitySimilarity.Value * quantityWeight), 0d, 1d);
    }

    private static SourceScoreProfile GetScoreProfile(TaskResourceSuggestionSource source)
        => source == TaskResourceSuggestionSource.BlueprintTask
            ? BlueprintScoreProfile
            : TenantScoreProfile;

    private static string BuildReason(CandidateScoreBreakdown score, TaskResourceSuggestionSource source)
    {
        var sourceText = source == TaskResourceSuggestionSource.BlueprintTask
            ? "TaskResourceBlueprints"
            : "Tenant task history";

        var strength = score.Score switch
        {
            >= 0.99d => "Direct match",
            >= 0.90d => "Very strong match",
            >= 0.75d => "Strong match",
            >= 0.55d => "Moderate match",
            _ => "Weak match"
        };

        var parts = new List<string>(8)
        {
            $"text {FormatPercent(score.TextScore)}",
            $"name {FormatPercent(score.NameScore)}"
        };

        if (score.CodeBonus > 0)
            parts.Add($"code +{FormatPercent(score.CodeBonus)}");

        if (score.HybridTokenScore >= 0.40d)
            parts.Add($"keyword coverage {FormatPercent(score.TokenCoverage)}");

        if (score.ParentContextBonus > 0)
            parts.Add($"parent context +{FormatPercent(score.ParentContextBonus)}");

        if (score.HasEquivalentUnits)
            parts.Add("same unit");
        else if (score.HasCompatibleUnits)
            parts.Add("compatible unit");

        if (source == TaskResourceSuggestionSource.TenantTask && score.QuantitySimilarity.HasValue)
            parts.Add($"quantity {FormatPercent(score.QuantitySimilarity.Value)}");

        if (score.MlScore.HasValue)
        {
            var mlSource = source == TaskResourceSuggestionSource.TenantTask
                ? "tenant ML"
                : "blueprint/global ML";
            parts.Add($"{mlSource} {FormatPercent(score.MlScore.Value)}");
        }

        if (!string.IsNullOrWhiteSpace(score.ContextReason))
            parts.Add($"parent/code context: {score.ContextReason}");

        return $"{strength} from {sourceText}: {string.Join(", ", parts)}.";
    }

    private static List<TaskResourceSuggestionScoreDetailDTO> BuildScoreDetails(
        CandidateScoreBreakdown score,
        TaskResourceSuggestionSource source)
    {
        var details = new List<TaskResourceSuggestionScoreDetailDTO>
        {
            BuildScoreDetail("Name", score.NameScore, "name"),
            BuildScoreDetail("Text", score.TextScore, "text")
        };

        if (score.CodeBonus > 0)
            details.Add(BuildScoreDetail("Code", score.CodeBonus, "code"));

        if (score.HasEquivalentUnits)
            details.Add(new TaskResourceSuggestionScoreDetailDTO { Label = "Same unit", Kind = "unit" });
        else if (score.HasCompatibleUnits)
            details.Add(new TaskResourceSuggestionScoreDetailDTO { Label = "Compatible unit", Kind = "unit" });
        else if (score.UnitBonus < 0)
            details.Add(BuildScoreDetail("Unit mismatch", score.UnitBonus, "warning"));

        if (source == TaskResourceSuggestionSource.TenantTask && score.QuantitySimilarity.HasValue)
            details.Add(BuildScoreDetail("Quantity", score.QuantitySimilarity.Value, "quantity"));

        if (score.ParentContextBonus > 0)
            details.Add(BuildScoreDetail("Parent", score.ParentContextBonus, "context"));

        if (score.ParentNameBonus > 0)
            details.Add(BuildScoreDetail("Parent match", score.ParentNameBonus, "context"));

        if (score.SiblingBonus > 0)
            details.Add(BuildScoreDetail("Sibling", score.SiblingBonus, "context"));

        if (score.HybridTokenScore >= 0.40d)
            details.Add(BuildScoreDetail("Keywords", score.TokenCoverage, "keyword"));

        if (score.MlScore.HasValue)
            details.Add(BuildScoreDetail(source == TaskResourceSuggestionSource.TenantTask ? "Tenant ML" : "Global ML", score.MlScore.Value, "ml"));

        return details;
    }

    private static TaskResourceSuggestionScoreDetailDTO BuildScoreDetail(string label, double value, string kind)
        => new()
        {
            Label = label,
            Value = Math.Round(value, 4),
            Kind = kind
        };

    private static string FormatPercent(double value)
        => value.ToString("0%", System.Globalization.CultureInfo.InvariantCulture);

    private static string FormatSignedPercent(double value)
        => value >= 0
            ? $"+{FormatPercent(value)}"
            : FormatPercent(value);

    private static string Limit(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength].Trim();

    private sealed record CandidateScoreBreakdown(
        double Score,
        double TextScore,
        double NameScore,
        double TokenCoverage,
        double HybridTokenScore,
        double? QuantitySimilarity,
        double UnitBonus,
        double CodeBonus,
        double ParentContextBonus,
        double ParentNameBonus,
        double SiblingBonus,
        bool HasEquivalentUnits,
        bool HasCompatibleUnits)
    {
        public double? MlScore { get; init; }
        public double MlBlendWeight { get; init; }
        public string? ContextReason { get; init; }
    }

    private sealed record SourceScoreProfile(
        double QuantityWeight,
        double EquivalentUnitBonus,
        double CompatibleUnitBonus,
        double IncompatibleUnitPenalty,
        double CodeWeightMultiplier,
        double ParentContextMultiplier);

    private sealed record FeedbackStats(
        int Accepted,
        int Rejected,
        int WrongUnit,
        int WrongResourceType,
        int MissingResources)
    {
        public int Total => Accepted + Rejected + WrongUnit + WrongResourceType + MissingResources;
    }

    private sealed record TenantTaskAncestorContext(
        int Id,
        int? ParentTaskId,
        string Name,
        string? Code);

    private sealed record TenantTaskSuggestionContext(
        int Id,
        string Name,
        string ContextualName,
        string NormalizedText,
        string ParentNormalizedText,
        string? ParentName,
        int? ParentTaskId,
        string? Unit,
        string? Code,
        int CodeDepth,
        decimal? Quantity);

    private sealed record BlueprintTaskSuggestionContext(
        string Name,
        string ContextualName,
        string NormalizedText,
        string ParentNormalizedText,
        string? ParentName,
        string? Unit,
        string? Code,
        int CodeDepth,
        decimal? Quantity);

    private static ResourcePostDTO ToResourcePostDto(ResourceEntity resource)
    {
        return new ResourcePostDTO
        {
            Name = resource.Name,
            IsActive = resource.IsActive,
            ResType = resource.ResType,
            Quantity = resource.Quantity ?? 0m,
            Unit = resource.Unit,
            AccountId = resource.AccountId,
            StatusId = resource.StatusId,
            ResourceSortId = resource.ResourceSortId,
            ResourceTypeId = resource.ResourceTypeId,
            OpportunityId = resource.OpportunityId,
            SortOrder = resource.SortOrder,
            Data = resource.GetMetadataSnapshot()
        };
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<TaskResourceSuggestionDTO>>> GetBulkSuggestionsAsync(
        IReadOnlyList<int> taskIds,
        int maxResultsPerTask,
        CancellationToken cancellationToken = default)
    {
        maxResultsPerTask = Math.Clamp(maxResultsPerTask, 1, 20);

        if (taskIds.Count == 0)
            return new Dictionary<int, IReadOnlyList<TaskResourceSuggestionDTO>>();

        // Pre-fetch global feedback stats once — shared across all N tasks to avoid N GROUP BY queries
        await using var sharedTenantDb = await dbFactory.CreateDbContextAsync(cancellationToken);
        var sharedFeedbackStats = await GetFeedbackStatsMapAsync(sharedTenantDb.TenantId, cancellationToken);

        using var sem = new SemaphoreSlim(4);
        var pairs = await System.Threading.Tasks.Task.WhenAll(taskIds.Select(async taskId =>
        {
            await sem.WaitAsync(cancellationToken);
            try
            {
                var suggestions = await GetSuggestionsInternalAsync(taskId, maxResultsPerTask, includeResources: false, sharedFeedbackStats, cancellationToken);
                return (taskId, suggestions);
            }
            finally { sem.Release(); }
        }));

        return pairs.ToDictionary(p => p.taskId, p => p.suggestions);
    }

}
