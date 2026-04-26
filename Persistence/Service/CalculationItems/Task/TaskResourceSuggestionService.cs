using Application.Feature.Calculation.Task;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Helper.Text;
using TaskResourceBlueprints.Entities.Questions.Assignments;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;

namespace Persistence.Service.CalculationItems.Task;

public sealed class TaskResourceSuggestionService(
    IDbContextFactoryTenant dbFactory,
    IDbContextFactory<TaskResourceBlueprintsContext> blueprintFactory) : ITaskResourceSuggestionService
{
    private const double MinimumScore = 0.30d;

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

        var targetNormalized = !string.IsNullOrWhiteSpace(target.NormalizedTextSv)
            ? target.NormalizedTextSv
            : SwedishTaskTextNormalizer.NormalizeTask(target.Name, target.Code, target.Unit, target.Metadata.Quantity);

        if (string.IsNullOrWhiteSpace(targetNormalized))
            return [];

        var tenantTask = GetTenantTaskSuggestionsAsync(
            tenantDb,
            target.Id,
            targetNormalized,
            target.Unit,
            target.Code,
            target.Metadata.Quantity,
            maxResults,
            includeResources,
            cancellationToken);

        var blueprintTask = GetBlueprintSuggestionsAsync(
            tenantDb.TenantId,
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
        string targetNormalized,
        string? targetUnit,
        string? targetCode,
        decimal? targetQuantity,
        int maxResults,
        bool includeResources,
        CancellationToken ct)
    {
        var query = db.Tasks
            .AsNoTracking()
            .Where(x => x.Id != targetTaskId && x.Resources.Any())
            .OrderByDescending(x => x.Id)
            .Take(500);

        if (includeResources)
            query = query.Include(x => x.Resources);

        var candidates = await query
            .ToListAsync(ct);

        return candidates
            .Select(task =>
            {
                var candidateNormalized = !string.IsNullOrWhiteSpace(task.NormalizedTextSv)
                    ? task.NormalizedTextSv
                    : SwedishTaskTextNormalizer.NormalizeTask(task.Name, task.Code, task.Unit, task.Metadata.Quantity);

                var score = ScoreCandidate(
                    targetNormalized,
                    candidateNormalized,
                    targetUnit,
                    task.Unit,
                    targetCode,
                    task.Code,
                    targetQuantity,
                    task.Metadata.Quantity);

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
        string targetNormalized,
        string? targetUnit,
        string? targetCode,
        decimal? targetQuantity,
        int maxResults,
        bool includeResources,
        CancellationToken ct)
    {
        await using var db = await blueprintFactory.CreateDbContextAsync(ct);

        var query = db.Tasks
            .AsNoTracking()
            .Where(x => x.Status == TaskStatusEnum.Ready && x.TaskResourceAssignments.Any())
            .OrderBy(x => x.SortOrder)
            .Take(500);

        if (includeResources)
        {
            query = query
                .Include(x => x.TaskResourceAssignments)
                    .ThenInclude(x => x.Resource)
                        .ThenInclude(x => x!.TenantLinks);
        }

        var candidates = await query
            .ToListAsync(ct);

        return candidates
            .Select(task =>
            {
                var candidateNormalized = !string.IsNullOrWhiteSpace(task.NormalizedTextSv)
                    ? task.NormalizedTextSv
                    : SwedishTaskTextNormalizer.NormalizeTask(task.Name, task.Code, task.UnitCode, task.Quantity);

                var score = ScoreCandidate(
                    targetNormalized,
                    candidateNormalized,
                    targetUnit,
                    task.UnitCode,
                    targetCode,
                    task.Code,
                    targetQuantity,
                    task.Quantity);

                return new TaskResourceSuggestionDTO
                {
                    SourceTaskId = task.Id,
                    SourceTaskName = task.Name,
                    SourceTaskQuantity = task.Quantity,
                    SourceTaskUnit = task.UnitCode ?? string.Empty,
                    Source = TaskResourceSuggestionSource.BlueprintTask,
                    Score = score,
                    Reason = BuildReason(score, TaskResourceSuggestionSource.BlueprintTask),
                    Resources = includeResources
                        ? task.TaskResourceAssignments
                            .OrderBy(x => x.Resource != null ? x.Resource.SortOrder : 0)
                            .Select(x => ToResourcePostDto(x, tenantId))
                            .Where(x => x is not null)
                            .Select(x => x!)
                            .ToList()
                        : []
                };
            })
            .Where(x => x.Score >= MinimumScore)
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .ToList();
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
            .Include(x => x.TaskResourceAssignments)
                .ThenInclude(x => x.Resource)
                    .ThenInclude(x => x!.TenantLinks)
            .FirstOrDefaultAsync(ct);

        if (task is null)
            return [];

        return task.TaskResourceAssignments
            .OrderBy(x => x.Resource != null ? x.Resource.SortOrder : 0)
            .Select(x => ToResourcePostDto(x, tenantId))
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();
    }

    private static double ScoreCandidate(
        string targetNormalized,
        string candidateNormalized,
        string? targetUnit,
        string? candidateUnit,
        string? targetCode = null,
        string? candidateCode = null,
        decimal? targetQuantity = null,
        decimal? candidateQuantity = null)
    {
        var score = SwedishTaskTextNormalizer.CalculateSimilarity(targetNormalized, candidateNormalized);

        score = ApplyQuantityScore(score, targetQuantity, candidateQuantity);

        if (!string.IsNullOrWhiteSpace(targetUnit) &&
            !string.IsNullOrWhiteSpace(candidateUnit) &&
            string.Equals(targetUnit.Trim(), candidateUnit.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            score = Math.Min(1d, score + 0.08d);
        }

        if (HasCodePrefixMatch(targetCode, candidateCode))
        {
            score = Math.Min(1d, score + 0.05d);
        }

        return Math.Round(score, 4);
    }

    private static double ApplyQuantityScore(double currentScore, decimal? targetQuantity, decimal? candidateQuantity)
    {
        if (targetQuantity is not > 0m || candidateQuantity is not > 0m)
            return currentScore;

        var smaller = Math.Min(targetQuantity.Value, candidateQuantity.Value);
        var larger = Math.Max(targetQuantity.Value, candidateQuantity.Value);
        var quantitySimilarity = (double)(smaller / larger);

        return Math.Clamp((currentScore * 0.90d) + (quantitySimilarity * 0.10d), 0d, 1d);
    }

    private static bool HasCodePrefixMatch(string? targetCode, string? candidateCode)
    {
        if (string.IsNullOrWhiteSpace(targetCode) || string.IsNullOrWhiteSpace(candidateCode))
            return false;

        var targetPrefix = GetCodePrefix(targetCode.Trim());
        var candidatePrefix = GetCodePrefix(candidateCode.Trim());

        return targetPrefix.Length > 0 &&
               string.Equals(targetPrefix, candidatePrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetCodePrefix(string code)
    {
        var dotIndex = code.IndexOf('.');
        return dotIndex > 0 ? code[..dotIndex] : code.Length > 1 ? code[..1] : string.Empty;
    }

    private static string BuildReason(double score, TaskResourceSuggestionSource source)
    {
        var sourceText = source == TaskResourceSuggestionSource.BlueprintTask
            ? "TaskResourceBlueprints"
            : "Tenant task history";

        return score switch
        {
            >= 0.95d => $"Direct normalized match from {sourceText}.",
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

    private static ResourcePostDTO? ToResourcePostDto(TaskResourceAssignment assignment, int tenantId)
    {
        var resource = assignment.Resource;
        if (resource is null)
            return null;

        var tenantLink = resource.TenantLinks.FirstOrDefault(x => x.TenantId == tenantId);
        var data = CalculationItemMetadataMapper.CloneResourceMetadata(resource.Data);

        data.ChangeFactor1 = assignment.ChangeFactor1;
        data.ChangeFactor2 = assignment.ChangeFactor2;
        data.CapWaste = assignment.CapWaste;
        data.BaseCost = assignment.BaseCost;

        if (tenantLink?.Cost is not null)
            data.Cost = tenantLink.Cost.Value;

        if (tenantLink?.Co2 is not null)
            data.CO2 = tenantLink.Co2;

        return new ResourcePostDTO
        {
            Name = !string.IsNullOrWhiteSpace(tenantLink?.Name) ? tenantLink.Name : resource.Name,
            IsActive = assignment.IsActive && resource.IsActive,
            ResType = resource.ResType,
            AccountId = tenantLink?.AccountId,
            StatusId = tenantLink?.StatusId,
            ResourceTypeId = tenantLink?.ResourceTypeId,
            ResourceSortId = tenantLink?.ResourceSortId,
            SortOrder = resource.SortOrder,
            Data = data
        };
    }
}
