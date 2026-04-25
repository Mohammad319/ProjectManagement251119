using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Mappers;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using TaskResourceBlueprints.Dto.ProjectTask;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Lookups;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Entities.Tasks;
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
            Resources = x.TaskResourceAssignments.Select(res => new ResourceEXDto()
            {
                CapRole = res.CapacityRoles,
                Id = res.Resource != null ? res.Resource.Id : 0,
                Name = res.Resource != null ? res.Resource.Name : string.Empty,
                Data = new ResourceMetadata()
                {
                    ChangeFactor1 = res.ChangeFactor1,
                    ChangeFactor2 = res.ChangeFactor2,
                    CapWaste = res.CapWaste,
                    BaseCost = res.BaseCost,
                    Quantity = res.Resource != null && res.Resource.Data != null ? res.Resource.Data.Quantity : 0,
                    Unit = res.Resource != null && res.Resource.Data != null ? res.Resource.Data.Unit : string.Empty,
                    Cost = res.Resource != null && res.Resource.Data != null ? res.Resource.Data.Cost : 0,
                },
                Group = res.Resource != null && res.Resource.Folder != null ? res.Resource.Folder.DisplayName : string.Empty,
                ResType = res.Resource != null ? res.Resource.ResType : ResourceTypesEnum.Adjustment,
                SortOrder = res.Resource != null ? res.Resource.SortOrder : 0,
            }).ToList()
        };
    }
    public class TaskResourceDto
    {
        [JsonIgnore] public CalcResCost? CalcResCost;
        public int? MenuId { get; set; }
        public decimal ChangeFactor1 { get; set; } = 1;
        public decimal ChangeFactor2 { get; set; } = 1;
        public decimal CapWaste { get; set; } = 1;
        public decimal? BaseCost { get; set; } = 0;
        public bool Uncontrollable { get; set; } = false;
        public bool Active { get; set; } = false;
        public List<RoleDTO>? CapRole { get; set; } = [];
        public List<string> Formulas { get; set; } = [];
        public ResourceTypesEnum ResType { get; set; }
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
        Task<(IReadOnlyList<ActionEntity> actions,
      IReadOnlyList<ActionTypeEntity> actionTypes, IReadOnlyList<FallEntity> falls,
      IReadOnlyList<LocationEntity> locations, IReadOnlyList<TaskUnitGroup> unitGroups,
      IReadOnlyList<ResourceCategory> folders)> GetLookupsAsync(CancellationToken ct);
    }

    public sealed class ProjectTaskService(IDbContextFactory<TaskResourceBlueprintsContext> factory) : ITaskDefinitionService
    {
        public async Task<(IReadOnlyList<ActionEntity>, IReadOnlyList<ActionTypeEntity>,
                   IReadOnlyList<FallEntity>, IReadOnlyList<LocationEntity>, IReadOnlyList<TaskUnitGroup>,
                   IReadOnlyList<ResourceCategory>)> GetLookupsAsync(CancellationToken ct)
        {
            async Task<List<T>> Run<T>(Func<TaskResourceBlueprintsContext, IQueryable<T>> query) where T : class
            {
                await using var db = await factory.CreateDbContextAsync(ct);
                return await query(db).AsNoTracking().ToListAsync(ct);
            }


            var actionsTask = Run(db => db.Actions.OrderBy(x => x.SortOrder).ThenBy(x => x.Name));
            var actionTypesTask = Run(db => db.ActionTypes.OrderBy(x => x.SortOrder).ThenBy(x => x.Name));
            var fallsTask = Run(db => db.Falls.OrderBy(x => x.SortOrder).ThenBy(x => x.Name));
            var locationsTask = Run(db => db.Locations.OrderBy(x => x.SortOrder).ThenBy(x => x.Name));
            var unitGroupsTask = Run(db => db.TaskUnitGroups.OrderBy(x => x.DisplayName));
            var foldersTask = Run(db => db.ResourceCategories.OrderBy(x => x.SortOrder).ThenBy(x => x.DisplayName));

            await Task.WhenAll(actionsTask, actionTypesTask, fallsTask, locationsTask, unitGroupsTask, foldersTask);

            return (await actionsTask, await actionTypesTask, await fallsTask,
                    await locationsTask, await unitGroupsTask, await foldersTask);
        }
        public async Task<List<TaskWithResourcesMDto>?> GetTasksWithAdjustedResources(int? ActionId, int? LocationId, int? FallId, int? ActionTypeId)
        {
            await using var context = factory.CreateDbContext();
            var tasks = await context.Tasks.AsNoTracking()
                .Where(t =>
                    (!ActionId.HasValue || t.ActionId == ActionId) &&
                    (!LocationId.HasValue || t.LocationId == LocationId) &&
                    (!FallId.HasValue || t.FallId == FallId) &&
                    (!ActionTypeId.HasValue || t.ActionTypeId == ActionTypeId)).Select(TaskSelectors.WithResources).ToListAsync();
            return tasks;
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
            return await query.TasksBaseToDto(tenantid).ToListAsync(ct);
        }
        public async Task<ProjectTaskDto?> GetTaskForUserDtoAsync(int id, int tenantid, int depId, CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            return await db.Tasks
                .Where(x => x.Status == TaskStatusEnum.Ready)
                .AsNoTracking()
                .ProjectToDto(tenantid, depId)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
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
                ActionId = d.ActionId,
                ActionTypeId = d.ActionTypeId,
                Responsible = d.Responsible,
                Status = d.Status,
                AdminNote = d.AdminNote,
                FallId = d.FallId,
                LocationId = d.LocationId,
                Code = d.Code,
                Name = d.DisplayName,
                TaskUnitGroupId = d.UnitGroupId,
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
            return e.Id;
        }

        // Deutsch: Update – Entity innerhalb desselben DbContext laden & speichern
        public async Task UpdateAsync(TaskDefinitionEditDto d, CancellationToken ct)
        {
            Validate(d);

            await using var db = await factory.CreateDbContextAsync(ct);

            var e = await db.Tasks.FirstAsync(x => x.Id == d.Id, ct);

            e.ActionId = d.ActionId;
            e.Responsible = d.Responsible;
            e.Status = d.Status;
            e.AdminNote = d.AdminNote;
            e.Uncontrollable = d.Uncontrollable;

            e.ActionTypeId = d.ActionTypeId;
            e.FallId = d.FallId;
            e.LocationId = d.LocationId;
            e.Code = d.Code;
            e.Name = d.DisplayName;
            e.TaskUnitGroupId = d.UnitGroupId;
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
