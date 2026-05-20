using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper.Text;
using ProjectManagement.Shared.Mappers;
using System.Linq.Expressions;
using TaskResourceBlueprints.Dto.ProjectTask;
using TaskResourceBlueprints.Entities.Lookups;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Infrastructure;
using TaskResourceBlueprints.Mappers.Shared.Mappers;
using TaskResourceBlueprints.Services.Import;
namespace TaskResourceBlueprints.Services.ProjectTask
{
    public static class TaskSelectors
    {
        public static Expression<Func<TaskDefinition, TaskWithResourcesMDto>> WithResources => x => new TaskWithResourcesMDto()
        {
            Id = x.Id,
            Name = x.Name,
            Code = x.Code ?? string.Empty,
            ChangeFactor1 = x.ChangeFactor1,
            ChangeFactor2 = x.ChangeFactor2,
            ResIdCap = x.CapacityResourceId,
            Note = x.FieldNotes ?? string.Empty,
            Quantity = x.Quantity,
            Unit = x.UnitCode ?? string.Empty,
            Resources = new List<ResourceEXDto>()
        };
    }
    public class TaskResourceDto
    {
        public int? MenuId { get; set; }
        public decimal ChangeFactor1 { get; set; } = 1;
        public decimal ChangeFactor2 { get; set; } = 1;
        public bool IsFixed { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal CapWaste { get; set; } = 1;
        public decimal? BaseCost { get; set; } = 0;
        public bool Uncontrollable { get; set; } = false;
        public bool Active { get; set; } = false;
        public List<RoleDTO>? CapRole { get; set; } = [];
        public ResourceTypesEnum ResType { get; set; }
        public List<ResourceParameter> Parameters { get; set; } = [];
        public List<ResourceAddon> AddOns { get; set; } = [];
        public List<ResourceTime> Times { get; set; } = [];
        public string Unit { get; set; } = string.Empty;
    }
    public interface ITaskDefinitionService
    {
        Task<bool> UpdateResourceAppStorageTenantAsync(int tenantId, int ResourceId, ResourceTenantLinkBase taskResourceDto, CancellationToken ct);
        Task<List<TaskWithResourcesMDto>?> GetTasksWithAdjustedResources(int? ActionId, int? LocationId, int? FallId, int? ActionTypeId);
        Task<List<ProjectTaskDto>> GetTasksForUserDtoAsync(ProjectTaskFilterDto filter, int tenantid, CancellationToken ct);
        Task<ProjectTaskDto?> GetTaskForUserDtoAsync(int id, int tenantid, int depId, CancellationToken ct);
        Task<int> CreateAsync(TaskDefinitionEditDto dto, CancellationToken ct);
        Task UpdateAsync(TaskDefinitionEditDto dto, CancellationToken ct);
        Task DeleteAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<ResourceCategory>> GetLookupsAsync(CancellationToken ct);
        Task UpdateVisibleFoldersAsync(int taskId, List<int> folderIds, CancellationToken ct = default);
        Task<int> RebuildNormalizedTextAsync(CancellationToken ct = default);
        Task IncrementUsageAsync(int taskId, CancellationToken ct = default);
    }

    public sealed class ProjectTaskService(IDbContextFactory<TaskResourceBlueprintsContext> factory) : ITaskDefinitionService
    {
        private const int SearchCandidateLimit = 2000;

        public async Task<IReadOnlyList<ResourceCategory>> GetLookupsAsync(CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            return await db.ResourceCategories.AsNoTracking()
                .OrderBy(x => x.SortOrder).ThenBy(x => x.DisplayName)
                .ToListAsync(ct);
        }
        public async Task<List<TaskWithResourcesMDto>?> GetTasksWithAdjustedResources(int? ActionId, int? LocationId, int? FallId, int? ActionTypeId)
        {
            await using var context = factory.CreateDbContext();
            return await context.Tasks.AsNoTracking()
                .Select(TaskSelectors.WithResources)
                .ToListAsync();
        }
        public async Task<List<ProjectTaskDto>> GetTasksForUserDtoAsync(ProjectTaskFilterDto filter, int tenantid, CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var query = db.Tasks.Where(x => x.Status == TaskStatusEnum.Ready).AsNoTracking().AsQueryable();

            if (filter.ResourcesOnly)
            {
                query = query.Where(x => x.ResourceLinks.Any(l =>
                    l.Resource != null &&
                    l.Resource.IsActive &&
                    l.Resource.IsVisible));
            }

            var tokens = filter.SearchTokens
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(4)
                .ToList();

            if (tokens.Count > 0)
            {
                // OR-search across NormalizedTextSv (Swedish-normalized), Name, and Code.
                // Token[2] when present is a bigram (e.g. "schakt_planteringsyta") — searched
                // only in NormalizedTextSv where bigrams are stored with _ separator.
                var t0 = tokens[0];
                if (tokens.Count == 1)
                {
                    query = query.Where(x =>
                        x.NormalizedTextSv.Contains(t0) ||
                        x.Name.Contains(t0) ||
                        (x.Code ?? string.Empty).Contains(t0));
                }
                else if (tokens.Count == 2)
                {
                    var t1 = tokens[1];
                    query = query.Where(x =>
                        x.NormalizedTextSv.Contains(t0) || x.Name.Contains(t0) || (x.Code ?? string.Empty).Contains(t0) ||
                        x.NormalizedTextSv.Contains(t1) || x.Name.Contains(t1) || (x.Code ?? string.Empty).Contains(t1));
                }
                else if (tokens.Count == 3)
                {
                    var t1 = tokens[1];
                    var t2 = tokens[2]; // bigram phrase — only in NormalizedTextSv
                    query = query.Where(x =>
                        x.NormalizedTextSv.Contains(t0) || x.Name.Contains(t0) || (x.Code ?? string.Empty).Contains(t0) ||
                        x.NormalizedTextSv.Contains(t1) || x.Name.Contains(t1) || (x.Code ?? string.Empty).Contains(t1) ||
                        x.NormalizedTextSv.Contains(t2));
                }
                else
                {
                    var t1 = tokens[1];
                    var t2 = tokens[2];
                    var t3 = tokens[3];
                    query = query.Where(x =>
                        x.NormalizedTextSv.Contains(t0) || x.Name.Contains(t0) || (x.Code ?? string.Empty).Contains(t0) ||
                        x.NormalizedTextSv.Contains(t1) || x.Name.Contains(t1) || (x.Code ?? string.Empty).Contains(t1) ||
                        x.NormalizedTextSv.Contains(t2) ||
                        x.NormalizedTextSv.Contains(t3));
                }
            }
            else if (!string.IsNullOrWhiteSpace(filter.NameOrCode))
            {
                // Fallback for callers that still use the old single-string field.
                var fallbackSearch = filter.NameOrCode.Trim();
                query = query.Where(x =>
                    x.NormalizedTextSv.Contains(fallbackSearch) ||
                    x.Name.Contains(fallbackSearch) ||
                    (x.Code ?? string.Empty).Contains(fallbackSearch));
            }

            var searchContext = BuildTaskSearchContext(filter, tokens);
            if (searchContext.HasSearch)
                return await GetRankedTasksForUserDtoAsync(query, searchContext, filter, tenantid, ct);

            return await query
                .OrderByDescending(x => x.UsageCount)
                .ThenBy(x => x.Code)
                .ThenBy(x => x.Name)
                .Skip(filter.Skip)
                .Take(filter.Take)
                .TasksBaseToDto(tenantid)
                .ToListAsync(ct);
        }

        private static async Task<List<ProjectTaskDto>> GetRankedTasksForUserDtoAsync(
            IQueryable<TaskDefinition> query,
            TaskSearchContext search,
            ProjectTaskFilterDto filter,
            int tenantid,
            CancellationToken ct)
        {
            var candidates = await query
                .OrderByDescending(x => x.UsageCount)
                .ThenBy(x => x.Code)
                .ThenBy(x => x.Name)
                .Take(SearchCandidateLimit)
                .Select(x => new TaskSearchCandidate(
                    x.Id,
                    x.Name,
                    x.Code ?? string.Empty,
                    x.NormalizedTextSv,
                    x.UsageCount))
                .ToListAsync(ct);

            var candidateById = candidates.ToDictionary(x => x.Id);
            var rankedIds = candidates
                .Select(candidate => (candidate.Id, Score: CalculateTaskSearchScore(search, candidate)))
                .OrderByDescending(x => x.Score)
                .ThenBy(x => candidateById[x.Id].Code)
                .ThenBy(x => candidateById[x.Id].Name)
                .Skip(filter.Skip)
                .Take(filter.Take)
                .Select((x, index) => new { x.Id, index })
                .ToList();

            if (rankedIds.Count == 0)
                return [];

            var idOrder = rankedIds.ToDictionary(x => x.Id, x => x.index);
            var ids = rankedIds.Select(x => x.Id).ToList();
            var tasks = await query
                .Where(x => ids.Contains(x.Id))
                .TasksBaseToDto(tenantid)
                .ToListAsync(ct);

            return tasks
                .OrderBy(x => idOrder.GetValueOrDefault(x.Id, int.MaxValue))
                .ToList();
        }

        private static TaskSearchContext BuildTaskSearchContext(ProjectTaskFilterDto filter, IReadOnlyList<string> tokens)
        {
            var normalizedTokens = tokens
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Select(token => token.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var wordTokens = normalizedTokens.Where(token => !token.Contains('_')).ToList();
            var bigrams = normalizedTokens.Where(token => token.Contains('_')).ToList();
            var rawQuery = !string.IsNullOrWhiteSpace(filter.NameOrCode)
                ? filter.NameOrCode.Trim()
                : string.Join(' ', wordTokens);
            var normalizedQuery = wordTokens.Count > 0
                ? string.Join(' ', wordTokens)
                : SwedishTaskTextNormalizer.Normalize(rawQuery);

            return new TaskSearchContext(
                RawQuery: rawQuery,
                NormalizedQuery: normalizedQuery,
                WordTokens: wordTokens,
                Bigrams: bigrams);
        }

        private static double CalculateTaskSearchScore(TaskSearchContext search, TaskSearchCandidate candidate)
        {
            if (!search.HasSearch)
                return UsageScore(candidate.UsageCount);

            var candidateCode = candidate.Code ?? string.Empty;
            var normalizedCandidateCode = SwedishTaskTextNormalizer.Normalize(candidateCode);
            var normalizedCandidate = candidate.NormalizedTextSv ?? string.Empty;
            var normalizedName = SwedishTaskTextNormalizer.Normalize(candidate.Name);
            var normalizedTarget = string.Join(' ', new[] { normalizedCandidateCode, normalizedName, normalizedCandidate }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            var textScore = SwedishTaskTextNormalizer.CalculateSimilarity(search.NormalizedQuery, normalizedTarget);
            var nameScore = SwedishTaskTextNormalizer.CalculateSimilarity(search.NormalizedQuery, normalizedName);
            var tokenCoverage = CalculateTokenCoverage(search.WordTokens, normalizedTarget);
            var bigramScore = search.Bigrams.Count == 0
                ? 0d
                : search.Bigrams.Count(bigram => normalizedCandidate.Contains(bigram, StringComparison.OrdinalIgnoreCase)) / (double)search.Bigrams.Count;
            var codeScore = CalculateCodeScore(search, candidateCode, normalizedCandidateCode);
            var usageScore = UsageScore(candidate.UsageCount);

            var score =
                (textScore * 0.34d) +
                (nameScore * 0.24d) +
                (tokenCoverage * 0.22d) +
                (bigramScore * 0.10d) +
                (codeScore * 0.08d) +
                (usageScore * 0.02d);

            if (codeScore >= 1d)
                score = Math.Max(score, 0.98d);
            else if (codeScore >= 0.75d)
                score = Math.Max(score, 0.86d);

            if (tokenCoverage >= 1d && textScore >= 0.65d)
                score = Math.Max(score, 0.88d + (bigramScore * 0.05d));

            if (textScore < 0.20d && nameScore < 0.20d && tokenCoverage < 0.5d)
                score = Math.Min(score, 0.35d);

            return Math.Round(Math.Clamp(score, 0d, 1d), 4);
        }

        private static double CalculateCodeScore(TaskSearchContext search, string candidateCode, string normalizedCandidateCode)
        {
            if (string.IsNullOrWhiteSpace(candidateCode))
                return 0d;

            var rawQuery = search.RawQuery.Trim();
            var normalizedQuery = SwedishTaskTextNormalizer.Normalize(rawQuery);

            if (!string.IsNullOrWhiteSpace(rawQuery) &&
                string.Equals(candidateCode, rawQuery, StringComparison.OrdinalIgnoreCase))
                return 1d;

            if (!string.IsNullOrWhiteSpace(normalizedQuery) &&
                string.Equals(normalizedCandidateCode, normalizedQuery, StringComparison.OrdinalIgnoreCase))
                return 1d;

            if (!string.IsNullOrWhiteSpace(rawQuery) &&
                candidateCode.StartsWith(rawQuery, StringComparison.OrdinalIgnoreCase))
                return 0.78d;

            if (search.WordTokens.Count > 0 && search.WordTokens.All(token =>
                    normalizedCandidateCode.Contains(token, StringComparison.OrdinalIgnoreCase)))
                return 0.70d;

            return search.WordTokens.Any(token => normalizedCandidateCode.Contains(token, StringComparison.OrdinalIgnoreCase))
                ? 0.35d
                : 0d;
        }

        private static double CalculateTokenCoverage(IReadOnlyList<string> tokens, string normalizedTarget)
        {
            if (tokens.Count == 0)
                return 0d;

            return tokens.Count(token => normalizedTarget.Contains(token, StringComparison.OrdinalIgnoreCase)) / (double)tokens.Count;
        }

        private static double UsageScore(int usageCount)
            => usageCount <= 0 ? 0d : Math.Min(Math.Log10(usageCount + 1) / 3d, 1d);

        private sealed record TaskSearchContext(
            string RawQuery,
            string NormalizedQuery,
            IReadOnlyList<string> WordTokens,
            IReadOnlyList<string> Bigrams)
        {
            public bool HasSearch => !string.IsNullOrWhiteSpace(RawQuery)
                || !string.IsNullOrWhiteSpace(NormalizedQuery)
                || WordTokens.Count > 0
                || Bigrams.Count > 0;
        }

        private sealed record TaskSearchCandidate(
            int Id,
            string Name,
            string Code,
            string NormalizedTextSv,
            int UsageCount);
        public async Task<ProjectTaskDto?> GetTaskForUserDtoAsync(int id, int tenantid, int depId, CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var task = await db.Tasks
                .Where(x => x.Status == TaskStatusEnum.Ready)
                .AsNoTracking()
                .ProjectToDto(tenantid, depId)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (task is null)
                return null;

            task.BaseResources = await GetTaskBaseResourcesAsync(db, id, tenantid, ct);
            return task;
        }

        private static async Task<List<ResourceDto>> GetTaskBaseResourcesAsync(
            TaskResourceBlueprintsContext db,
            int taskId,
            int tenantId,
            CancellationToken ct)
        {
            var links = await db.TaskDefinitionResourceLinks
                .AsNoTracking()
                .Where(l => l.TaskDefinitionId == taskId && l.Resource != null && l.Resource.IsActive && l.Resource.IsVisible)
                .Include(l => l.Resource!)
                    .ThenInclude(r => r.TenantLinks)
                .OrderBy(l => l.Resource!.SortOrder)
                .ThenBy(l => l.Resource!.Name)
                .ToListAsync(ct);

            return links
                .Where(l => l.Resource is not null)
                .Select(l =>
                {
                    var resource = l.Resource!;
                    var tenantLink = resource.TenantLinks.FirstOrDefault(t => t.TenantId == tenantId);
                    var data = resource.Data.Clone();

                    ApplyResourceLinkMetadata(data, l);

                    return new ResourceDto
                    {
                        Id = resource.Id,
                        FolderId = resource.FolderId,
                        Active = resource.IsActive,
                        Name = resource.Name,
                        NameUserValue = tenantLink?.Name ?? string.Empty,
                        SortOrder = resource.SortOrder,
                        ResType = resource.ResType,
                        ResourceSource = ResourceSource.Base,
                        Quantity = l.Quantity > 0 ? l.Quantity : null,
                        Data = data,
                        CostRole = resource.CostRoles,
                        CostStorageValue = resource.Data.Cost,
                        CostUserValue = tenantLink?.Cost,
                        StatusId = tenantLink?.StatusId,
                        ResourceTypeId = tenantLink?.ResourceTypeId,
                        ResourceSortId = tenantLink?.ResourceSortId,
                        AccountId = tenantLink?.AccountId,
                    };
                })
                .ToList();
        }

        private static void ApplyResourceLinkMetadata(ResourceMetadata data, TaskDefinitionResourceLink link)
        {
            if (link.Parameters?.Count > 0)
                data.Parameters = [.. link.Parameters.Select(p => new ResourceParameter
                {
                    Name = p.Name,
                    Unit = p.Unit,
                    Value = p.Value
                })];

            if (link.AddOns?.Count > 0)
                data.AddOns = [.. link.AddOns.Select(a => new ResourceAddon
                {
                    Name = a.Name,
                    Unit = a.Unit,
                    Type = a.Type,
                    Factor = a.Factor,
                    Cost = a.Cost,
                    BaseCost = a.BaseCost
                })];

            if (link.Times?.Count > 0)
                data.Times = [.. link.Times.Select(t => new ResourceTime
                {
                    Name = t.Name,
                    Unit = t.Unit,
                    Percentage = t.Percentage,
                    Cost = t.Cost
                }.SetResolvedQuantity(t.Quantity))];
        }
        private static void Validate(TaskDefinitionEditDto d)
        {
            if (string.IsNullOrWhiteSpace(d.DisplayName))
                throw new ArgumentException("Name ist erforderlich.");
            if (d.Quantity is < 0)
                throw new ArgumentException("Quantity darf nicht negativ sein.");
            if (d.PriceProduction is < 0)
                throw new ArgumentException("Price production darf nicht negativ sein.");
            if (d.ChangeFactor1 <= 0 || d.ChangeFactor2 <= 0)
                throw new ArgumentException("Faktoren müssen > 0 sein.");
        }

        // Deutsch: Create – eigener DbContext-Scope, kein Parallelismus
        public async Task<int> CreateAsync(TaskDefinitionEditDto d, CancellationToken ct)
        {
            Validate(d);

            await using var db = await factory.CreateDbContextAsync(ct);

            var e = new TaskDefinition
            {
                Responsible = d.Responsible,
                Status = d.Status,
                AdminNote = d.AdminNote,
                Code = d.Code,
                Name = d.DisplayName,
                UnitCode = d.UnitCode,
                Quantity = d.Quantity,
                PriceProduction = d.PriceProduction,
                ChangeFactor1 = d.ChangeFactor1,
                ChangeFactor2 = d.ChangeFactor2,
                IsActive = d.IsActive,
                FieldNotes = d.Note,
                VisibleFolderIds = d.VisibleFolderIds?.ToList() ?? [],
                Uncontrollable = d.Uncontrollable,

                WorkloadThresholds =
                [
                    d.Thickness ?? 0,
                d.Width     ?? 0,
                d.Length    ?? 0
                ]
            };

            e.RefreshNormalizedTextSv();

            db.Tasks.Add(e);
            await db.SaveChangesAsync(ct);

            if (d.SelectedStateIds?.Count > 0)
            {
                foreach (var stateId in d.SelectedStateIds.Distinct())
                    db.TaskDefinitionStateLinks.Add(new TaskResourceBlueprints.Entities.Lookups.TaskDefinitionStateLink { TaskDefinitionId = e.Id, TaskStateId = stateId });
                await db.SaveChangesAsync(ct);
            }

            return e.Id;
        }

        // Deutsch: Update – Entity innerhalb desselben DbContext laden & speichern
        public async Task UpdateAsync(TaskDefinitionEditDto d, CancellationToken ct)
        {
            Validate(d);

            await using var db = await factory.CreateDbContextAsync(ct);

            var e = await db.Tasks
                .Include(t => t.StateLinks)
                .FirstAsync(x => x.Id == d.Id, ct);

            e.Responsible = d.Responsible;
            e.Status = d.Status;
            e.AdminNote = d.AdminNote;
            e.Uncontrollable = d.Uncontrollable;
            e.Code = d.Code;
            e.Name = d.DisplayName;
            e.UnitCode = d.UnitCode;
            e.Quantity = d.Quantity;
            e.PriceProduction = d.PriceProduction;
            e.ChangeFactor1 = d.ChangeFactor1;
            e.ChangeFactor2 = d.ChangeFactor2;
            e.IsActive = d.IsActive;
            e.FieldNotes = d.Note;
            e.VisibleFolderIds = d.VisibleFolderIds?.ToList() ?? [];
            e.WorkloadThresholds =
            [
                d.Thickness ?? 0,
            d.Width     ?? 0,
            d.Length    ?? 0
            ];
            e.RefreshNormalizedTextSv();

            // Sync state links
            var incomingIds = (d.SelectedStateIds ?? []).Distinct().ToHashSet();
            var existingIds = e.StateLinks.Select(l => l.TaskStateId).ToHashSet();
            foreach (var link in e.StateLinks.Where(l => !incomingIds.Contains(l.TaskStateId)).ToList())
                db.TaskDefinitionStateLinks.Remove(link);
            foreach (var stateId in incomingIds.Where(id => !existingIds.Contains(id)))
                db.TaskDefinitionStateLinks.Add(new TaskResourceBlueprints.Entities.Lookups.TaskDefinitionStateLink { TaskDefinitionId = e.Id, TaskStateId = stateId });

            await db.SaveChangesAsync(ct);
        }

        // Deutsch: Delete – einfaches Löschen
        public async Task DeleteAsync(int id, CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);

            var e = await db.Tasks.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return;

            db.Tasks.Remove(e);
            await db.SaveChangesAsync(ct);
        }

        public async Task UpdateVisibleFoldersAsync(int taskId, List<int> folderIds, CancellationToken ct = default)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var e = await db.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, ct);
            if (e is null) return;
            e.VisibleFolderIds = folderIds;
            await db.SaveChangesAsync(ct);
        }

        public async Task<int> RebuildNormalizedTextAsync(CancellationToken ct = default)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var tasks = await db.Tasks.ToListAsync(ct);
            var taskByCode = tasks
                .Where(t => !string.IsNullOrWhiteSpace(t.Code))
                .GroupBy(t => t.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var t in tasks)
            {
                TaskHierarchyContextBuilder.Apply(t, t.ParentCode, taskByCode);
                t.RefreshNormalizedTextSv();
            }
            return await db.SaveChangesAsync(ct);
        }

        public async Task IncrementUsageAsync(int taskId, CancellationToken ct = default)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var task = await db.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, ct);
            if (task is null) return;
            task.UsageCount++;
            await db.SaveChangesAsync(ct);
        }

        public async Task<bool> UpdateResourceAppStorageTenantAsync(int TenantId, int ResourceId, ResourceTenantLinkBase taskResourceDto, CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var zz = await db.ResourceTenantLinks.FirstOrDefaultAsync
                (x => x.TenantId == TenantId && x.ResourceId == ResourceId, ct);
            if (zz == null)
            {
                zz = new ResourceTenantLinkEntity
                {
                    TenantId = TenantId,
                    ResourceId = ResourceId,
                    Name = taskResourceDto.Name,
                    Co2 = taskResourceDto.Co2,
                    Cost = taskResourceDto.Cost,
                    StatusId = taskResourceDto.StatusId,
                    ResourceTypeId = taskResourceDto.ResourceTypeId,
                    ResourceSortId = taskResourceDto.ResourceSortId,
                    AccountId = taskResourceDto.AccountId,
                };
                db.ResourceTenantLinks.Add(zz);
            }
            else
            {
                zz.Name = taskResourceDto.Name;
                zz.Cost = taskResourceDto.Cost;
                zz.Co2 = taskResourceDto.Co2;

                zz.StatusId = taskResourceDto.StatusId;
                zz.ResourceTypeId = taskResourceDto.ResourceTypeId;
                zz.ResourceSortId = taskResourceDto.ResourceSortId;
                zz.AccountId = taskResourceDto.AccountId;
            }
            await db.SaveChangesAsync(ct);

            return true;
        }
    }

}
