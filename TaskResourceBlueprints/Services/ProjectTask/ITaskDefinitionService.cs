using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Mappers;
using System.Linq.Expressions;
using TaskResourceBlueprints.Dto.ProjectTask;
using TaskResourceBlueprints.Entities.Lookups;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Infrastructure;
using TaskResourceBlueprints.Mappers.Shared.Mappers;
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
    }

    public sealed class ProjectTaskService(IDbContextFactory<TaskResourceBlueprintsContext> factory) : ITaskDefinitionService
    {
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
            if (!string.IsNullOrEmpty(filter.NameOrCode))
            {
                var search = filter.NameOrCode;
                query = query.Where(x => (x.Code ?? string.Empty).Contains(search) ||
                x.Name.Contains(search));
            }
            return await query
                .OrderBy(x => x.Code).ThenBy(x => x.Name)
                .Skip(filter.Skip)
                .Take(filter.Take)
                .TasksBaseToDto(tenantid)
                .ToListAsync(ct);
        }
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

                    if (l.Quantity > 0)
                        data.Quantity = l.Quantity;

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
