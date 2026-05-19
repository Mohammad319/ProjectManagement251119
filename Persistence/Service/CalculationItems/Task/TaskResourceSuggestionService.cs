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
        var feedbackStatsBySuggestion = await GetFeedbackStatsMapAsync(tenantId, cancellationToken);

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
            cancellationToken);

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
            cancellationToken);

        await System.Threading.Tasks.Task.WhenAll(tenantTask, blueprintTask);

        return (await tenantTask)
            .Concat(await blueprintTask)
            .Select(x => ApplyFeedbackAdjustment(x, feedbackBySuggestion, feedbackStatsBySuggestion))
            .Where(x => !includeResources || x.Resources.Count > 0)
            .Where(x => x.Score >= MinimumScore)
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
        CancellationToken ct)
    {
        var baseQuery = db.Tasks
            .AsNoTracking()
            .Where(x => x.TenantId == db.TenantId && x.Id != targetTaskId && x.Resources.Any());

        var priorityQuery = ApplyTenantPriorityFilter(baseQuery, targetName, targetCode, targetUnit, targetNormalized)
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

        var candidateContexts = await BuildTenantTaskContextsAsync(db, candidates, ct);

        return candidates
            .Select(task =>
            {
                var candidateContext = candidateContexts[task.Id];
                var candidateNormalized = candidateContext.NormalizedText;

                var score = ApplyMlScore(
                    ExplainCandidateScoreWithCodeContext(
                    targetNormalized,
                    candidateNormalized,
                    targetUnit,
                    candidateContext.Unit,
                    targetCode,
                    candidateContext.Code,
                    targetQuantity,
                    candidateContext.Quantity,
                    targetName,
                    candidateContext.ContextualName,
                    targetCodeDepth,
                    candidateCodeDepth: candidateContext.CodeDepth)
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
                    SourceTaskQuantity = candidateContext.Quantity,
                    SourceTaskUnit = candidateContext.Unit ?? string.Empty,
                    Source = TaskResourceSuggestionSource.TenantTask,
                    Score = score.Score,
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
        int targetCodeDepth,
        decimal? targetQuantity,
        int maxResults,
        bool includeResources,
        ITaskResourceSuggestionMlRanker mlRanker,
        CancellationToken ct)
    {
        await using var db = await blueprintFactory.CreateDbContextAsync(ct);

        var baseQuery = db.Tasks
            .AsNoTracking()
            .Where(x =>
                x.Status == TaskStatusEnum.Ready &&
                x.ResourceLinks.Any(link =>
                    link.Resource != null &&
                    link.Resource.IsActive &&
                    link.Resource.IsVisible));

        var priorityQuery = ApplyBlueprintPriorityFilter(baseQuery, targetName, targetCode, targetUnit, targetNormalized)
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
                var candidateContext = BuildBlueprintTaskContext(task);

                var score = ApplyMlScore(
                    ExplainCandidateScoreWithCodeContext(
                    targetNormalized,
                    candidateContext.NormalizedText,
                    targetUnit,
                    candidateContext.Unit,
                    targetCode,
                    candidateContext.Code,
                    targetQuantity,
                    candidateContext.Quantity,
                    targetName,
                    candidateContext.ContextualName,
                    targetCodeDepth,
                    candidateCodeDepth: candidateContext.CodeDepth)
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
                    SourceTaskQuantity = candidateContext.Quantity,
                    SourceTaskUnit = candidateContext.Unit ?? string.Empty,
                    Source = TaskResourceSuggestionSource.BlueprintTask,
                    Score = score.Score,
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

        foreach (var ancestor in ancestors)
        {
            rawTextParts.Add(ancestor.Code ?? string.Empty);
            rawTextParts.Add(ancestor.Name);
        }

        rawTextParts.Add(effectiveCode ?? string.Empty);
        rawTextParts.Add(task.Name);

        // Unit is excluded from semantic text so compatible units don't penalize Jaccard.
        // Unit scoring is handled separately in ExplainCandidateScore.
        var normalizedText = SwedishTaskTextNormalizer.Normalize(string.Join(' ', rawTextParts));
        var contextualName = BuildContextualName(ancestors.Select(x => x.Name), task.Name);

        return new TenantTaskSuggestionContext(
            task.Id,
            task.Name,
            contextualName,
            normalizedText,
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
        var contextualName = BuildContextualName(
            SplitHierarchyNames(task.HierarchyPath, task.Name),
            task.Name);

        return new BlueprintTaskSuggestionContext(
            task.Name,
            contextualName,
            normalizedText,
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
        suggestion.Score = feedback.Feedback switch
        {
            TaskResourceSuggestionFeedbackKind.Accepted => Math.Round(Math.Min(1d, suggestion.Score + 0.08d), 4),
            TaskResourceSuggestionFeedbackKind.Rejected => Math.Round(suggestion.Score * 0.35d, 4),
            TaskResourceSuggestionFeedbackKind.WrongUnit => Math.Round(suggestion.Score * 0.55d, 4),
            TaskResourceSuggestionFeedbackKind.WrongResourceType => Math.Round(suggestion.Score * 0.60d, 4),
            TaskResourceSuggestionFeedbackKind.MissingResources => Math.Round(suggestion.Score * 0.80d, 4),
            _ => suggestion.Score
        };
        suggestion.Reason = $"{suggestion.Reason} Feedback: {feedback.Feedback}.";
        return suggestion;
    }

    private static void ApplyGlobalFeedbackAdjustment(
        TaskResourceSuggestionDTO suggestion,
        IReadOnlyDictionary<string, FeedbackStats> feedbackStatsBySuggestion)
    {
        if (!feedbackStatsBySuggestion.TryGetValue(FeedbackKey(suggestion.Source, suggestion.SourceTaskId), out var stats) ||
            stats.Total == 0)
            return;

        var positiveLift = Math.Min(0.06d, stats.Accepted * 0.015d);
        var negativePressure =
            Math.Min(0.18d,
                (stats.Rejected * 0.05d) +
                (stats.WrongUnit * 0.035d) +
                (stats.WrongResourceType * 0.035d) +
                (stats.MissingResources * 0.015d));

        var adjustment = positiveLift - negativePressure;
        if (Math.Abs(adjustment) < 0.005d)
            return;

        suggestion.Score = Math.Round(Math.Clamp(suggestion.Score + adjustment, 0d, 1d), 4);
        suggestion.Reason = $"{suggestion.Reason} Global feedback {FormatSignedPercent(adjustment)}.";
    }

    private static string FeedbackKey(TaskResourceSuggestionSource source, int sourceTaskId)
        => $"{source}:{sourceTaskId}";

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
            targetName,
            candidateName,
            codeInheritanceFactor: 1d,
            hasInheritedContext: false);

    private static CandidateScoreBreakdown ExplainCandidateScoreWithCodeContext(
        string targetNormalized,
        string candidateNormalized,
        string? targetUnit,
        string? candidateUnit,
        string? targetCode = null,
        string? candidateCode = null,
        decimal? targetQuantity = null,
        decimal? candidateQuantity = null,
        string? targetName = null,
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
            targetName,
            candidateName,
            GetCodeInheritanceFactor(targetCodeDepth, candidateCodeDepth),
            targetCodeDepth > 0 || candidateCodeDepth > 0);

    private static CandidateScoreBreakdown ExplainCandidateScoreCore(
        string targetNormalized,
        string candidateNormalized,
        string? targetUnit,
        string? candidateUnit,
        string? targetCode,
        string? candidateCode,
        decimal? targetQuantity,
        decimal? candidateQuantity,
        string? targetName,
        string? candidateName,
        double codeInheritanceFactor,
        bool hasInheritedContext)
    {
        var textScore = SwedishTaskTextNormalizer.CalculateSimilarity(targetNormalized, candidateNormalized);
        var nameScore = GetNameSimilarity(targetName, candidateName);
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

        score = ApplyQuantityScore(score, quantitySimilarity);

        var hasEquivalentUnits = QuantityUnitNormalizer.AreEquivalentUnits(targetUnit, candidateUnit);
        var hasCompatibleUnits = QuantityUnitNormalizer.AreCompatibleUnits(targetUnit, candidateUnit);
        var unitBonus = 0d;
        if (!string.IsNullOrWhiteSpace(targetUnit) && !string.IsNullOrWhiteSpace(candidateUnit))
        {
            if (hasEquivalentUnits)
                unitBonus = EquivalentUnitBonus;
            else if (hasCompatibleUnits)
                unitBonus = CompatibleUnitBonus;
            else
                unitBonus = -IncompatibleUnitPenalty;
        }

        score = Math.Clamp(score + unitBonus, 0d, 1d);

        var codeBonus = SwedishTaskTextNormalizer.GetCodeHierarchyScore(targetCode, candidateCode) * codeInheritanceFactor;
        score = Math.Min(1d, score + codeBonus);

        var parentContextBonus = 0d;
        if (hasInheritedContext &&
            codeBonus > 0d &&
            hybridTokenScore >= 0.25d)
        {
            parentContextBonus = ParentContextBonus * codeInheritanceFactor;
            score = Math.Min(1d, score + parentContextBonus);
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

        return Math.Clamp((currentScore * 0.82d) + (quantitySimilarity.Value * 0.18d), 0d, 1d);
    }

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

        var parts = new List<string>
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

        if (score.QuantitySimilarity.HasValue)
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
        bool HasEquivalentUnits,
        bool HasCompatibleUnits)
    {
        public double? MlScore { get; init; }
        public double MlBlendWeight { get; init; }
        public string? ContextReason { get; init; }
    }

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
        string? Unit,
        string? Code,
        int CodeDepth,
        decimal? Quantity);

    private sealed record BlueprintTaskSuggestionContext(
        string Name,
        string ContextualName,
        string NormalizedText,
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

        var sem = new SemaphoreSlim(4);
        var pairs = await System.Threading.Tasks.Task.WhenAll(taskIds.Select(async taskId =>
        {
            await sem.WaitAsync(cancellationToken);
            try
            {
                var suggestions = await GetSuggestionsAsync(taskId, maxResultsPerTask, includeResources: false, cancellationToken);
                return (taskId, suggestions);
            }
            finally { sem.Release(); }
        }));

        return pairs.ToDictionary(p => p.taskId, p => p.suggestions);
    }

}
