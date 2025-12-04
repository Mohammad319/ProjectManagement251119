using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using ProjectImportHub.Dto.ProjectTask;
using ProjectImportHub.Entities;
using ProjectImportHub.Infrastructure;
using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Mappers;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
namespace ProjectImportHub.Services.ProjectTask
{
    public static class TaskSelectors
    {
        public static Expression<Func<ProjectTaskEntity, TaskWithResourcesMDto>> WithResources => x => new TaskWithResourcesMDto()
        {
            Id = x.Id,
            Name = x.DisplayName,
            Code = x.Code,
            ChangeFactor1 = x.ChangeFactor1,
            ChangeFactor2 = x.ChangeFactor2,
            ResIdCap = x.CapacityResourceId,
            Note = x.Note,
            Quantity = x.Quantity,
            Unit = x.UnitCode,
            Resources = x.TaskResourceAssignments.Select(res => new ResourceEXDto()
            {
                CapRole = res.CapRole,
                Id = res.Resource.Id,
                Name = res.Resource.Name,
                Data = new ResourceData()
                {
                    ChangeFactor1 = res.ChangeFactor1,
                    ChangeFactor2 = res.ChangeFactor2,
                    CapWaste = res.CapWaste,
                    BaseCost = res.BaseCost,
                    Quantity = res.Resource.Data.Quantity,
                    Unit = res.Resource.Data.Unit,
                    Cost = res.Resource.Data.Cost,
                },
                Group = res.Resource.Folder != null ? res.Resource.Folder.DisplayName : string.Empty,
                ResType = res.Resource.ResType,
                SortOrder = res.Resource.SortOrder,
            }).ToList()
        };
    }
    public class TaskResourceDto
    {
        [JsonIgnore] public CalcResCost? CalcResCost;
        public int? MenuId { get; set; }
        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;
        public double CapWaste { get; set; } = 1;
        public double? BaseCost { get; set; } = 0;
        public bool Uncontrollable { get; set; } = false;
        public bool Active { get; set; } = false;
        public List<RoleDTO>? CapRole { get; set; } = [];
        public List<string> Formulas { get; set; } = [];
        public ResourceTypesEnum ResType { get; set; }
    }
    public interface IProjectTaskService
    {
        Task<bool> UpdateResourceAppStorageTenantAsync(int tenantId, int ResourceId, ResourceTenantLinkBase taskResourceDto, CancellationToken ct);
        Task<List<TaskWithResourcesMDto>?> GetTasksWithAdjustedResources(int? ActionId, int? LocationId, int? FallId, int? ActionTypeId);
        Task<List<ProjectTaskDto>> GetTasksForUserDtoAsync(ProjectTaskFilterDto filter,int tenantid,CancellationToken ct);
        Task<ProjectTaskDto> GetTaskForUserDtoAsync(int id, int tenantid, int depId, CancellationToken ct);
        Task<TaskResourceDto?> GetTaskResourceAsync(int TaskId, int ResourceId, CancellationToken ct);
        Task<bool> SaveTaskResourceAsync(int TaskId, int ResourceId, TaskResourceDto taskResourceDto, CancellationToken ct);
        Task<int> CreateAsync(ProjectTaskEditDto dto, CancellationToken ct);
        Task UpdateAsync(ProjectTaskEditDto dto, CancellationToken ct);
        Task DeleteAsync(int id, CancellationToken ct);
        Task<(IReadOnlyList<ActionEntity> actions,
      IReadOnlyList<ActionTypeEntity> actionTypes, IReadOnlyList<FallEntity> falls,
      IReadOnlyList<LocationEntity> locations, IReadOnlyList<UnitGroupEntity> unitGroups,
      IReadOnlyList<ResourceFolderEntity> folders)> GetLookupsAsync(CancellationToken ct);
    }

    public sealed class ProjectTaskService(IDbContextFactory<ProjectImportHubContext> factory) : IProjectTaskService
    {
        public async Task<(IReadOnlyList<ActionEntity>, IReadOnlyList<ActionTypeEntity>,
                   IReadOnlyList<FallEntity>, IReadOnlyList<LocationEntity>, IReadOnlyList<UnitGroupEntity>,
                   IReadOnlyList<ResourceFolderEntity>)> GetLookupsAsync(CancellationToken ct)
        {
            async Task<List<T>> Run<T>(Func<ProjectImportHubContext, IQueryable<T>> query) where T : class
            {
                await using var db = await factory.CreateDbContextAsync(ct);
                return await query(db).AsNoTracking().ToListAsync(ct);
            }


            var actionsTask = Run(db => db.Actions.OrderBy(x => x.DisplayName));
            var actionTypesTask = Run(db => db.ActionTypes.OrderBy(x => x.DisplayName));
            var fallsTask = Run(db => db.Falls.OrderBy(x => x.DisplayName));
            var locationsTask = Run(db => db.Locations.OrderBy(x => x.DisplayName));
            var unitGroupsTask = Run(db => db.UnitGroups.OrderBy(x => x.DisplayName));
            var foldersTask = Run(db => db.Folders.OrderBy(x => x.SortOrder));

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
            var query = db.Tasks.Where(x=>x.Status == TaskStatusEnum.Ready).AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(filter.NameOrCode))
            {
                query = query.Where(x => x.Code.Contains(filter.NameOrCode) ||
                x.DisplayName.Contains(filter.NameOrCode));
            }
            return await query.TasksBaseToDto(tenantid).ToListAsync(ct);
        }
        public async Task<ProjectTaskDto> GetTaskForUserDtoAsync(int id, int tenantid, int depId, CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            return await db.Tasks.Where(x => x.Status == TaskStatusEnum.Ready).AsNoTracking().ProjectToDto(tenantid, depId).FirstOrDefaultAsync(x => x.Id == id, ct);
        }
        private static void Validate(ProjectTaskEditDto d)
        {
            if (string.IsNullOrWhiteSpace(d.DisplayName))
                throw new ArgumentException("Name ist erforderlich.");
            if (d.Quantity is < 0)
                throw new ArgumentException("Quantity darf nicht negativ sein.");
            if (d.ChangeFactor1 <= 0 || d.ChangeFactor2 <= 0)
                throw new ArgumentException("Faktoren müssen > 0 sein.");
        }

        // Deutsch: Create – eigener DbContext-Scope, kein Parallelismus
        public async Task<int> CreateAsync(ProjectTaskEditDto d, CancellationToken ct)
        {
            Validate(d);

            await using var db = await factory.CreateDbContextAsync(ct);

            var e = new ProjectTaskEntity
            {
                ActionId = d.ActionId,
                ActionTypeId = d.ActionTypeId,
                Responsible = d.Responsible,
                Status = d.Status,
                AdminNote = d.AdminNote,
                FallId = d.FallId,
                LocationId = d.LocationId,
                Code = d.Code,
                DisplayName = d.DisplayName,
                UnitGroupId = d.UnitGroupId,
                UnitCode = d.UnitCode,
                Quantity = d.Quantity,
                ChangeFactor1 = d.ChangeFactor1,
                ChangeFactor2 = d.ChangeFactor2,
                IsActive = d.IsActive,
                Note = d.Note,
                VisibleFolderIds = d.VisibleFolderIds?.ToList() ?? [],
                Uncontrollable = d.Uncontrollable,
                
                WorkloadThresholds =
                [
                    d.Thickness ?? 0,
                d.Width     ?? 0,
                d.Length    ?? 0
                ]
            };

            db.Tasks.Add(e);
            await db.SaveChangesAsync(ct);
            return e.Id;
        }

        // Deutsch: Update – Entity innerhalb desselben DbContext laden & speichern
        public async Task UpdateAsync(ProjectTaskEditDto d, CancellationToken ct)
        {
            Validate(d);

            await using var db = await factory.CreateDbContextAsync(ct);

            var e = await db.Tasks.FirstAsync(x => x.Id == d.Id, ct);

            e.ActionId = d.ActionId;
            e.Responsible= d.Responsible;
            e.Status = d.Status;
            e.AdminNote = d.AdminNote;
            e.Uncontrollable = d.Uncontrollable;

            e.ActionTypeId = d.ActionTypeId;
            e.FallId = d.FallId;
            e.LocationId = d.LocationId;
            e.Code = d.Code;
            e.DisplayName = d.DisplayName;
            e.UnitGroupId = d.UnitGroupId;
            e.UnitCode = d.UnitCode;
            e.Quantity = d.Quantity;
            e.ChangeFactor1 = d.ChangeFactor1;
            e.ChangeFactor2 = d.ChangeFactor2;
            e.IsActive = d.IsActive;
            e.Note = d.Note;
            e.VisibleFolderIds = d.VisibleFolderIds?.ToList() ?? [];
            e.WorkloadThresholds =
            [
                d.Thickness ?? 0,
            d.Width     ?? 0,
            d.Length    ?? 0
            ];

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

        public async Task<TaskResourceDto?> GetTaskResourceAsync(int taskId,int resourceId,CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);

            var zz = await db.TaskResourceAssignments.AsNoTracking()
                .Where(x => x.TaskId == taskId && x.ResourceId == resourceId)
                .Select(x => new TaskResourceDto
                {
                    Active = x.Resource == null ? false : x.Resource.Active,
                    MenuId = x.MenuId,
                    BaseCost = x.BaseCost,
                    CapRole = x.CapRole,
                    CapWaste = x.CapWaste,
                    ChangeFactor1 = x.ChangeFactor1,
                    ChangeFactor2 = x.ChangeFactor2,
                    Formulas = x.Formulas,
                    ResType = x.Resource == null ? ResourceTypesEnum.Adjustment : x.Resource.ResType,
                    Uncontrollable = x.Uncontrollable,
                })
                .FirstOrDefaultAsync(ct);

            return zz;
        }

        public async Task<bool> SaveTaskResourceAsync(int TaskId, int ResourceId, TaskResourceDto taskResourceDto, CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var zz = await db.TaskResourceAssignments.FirstOrDefaultAsync
                (x => x.TaskId == TaskId && x.ResourceId == ResourceId, ct);
            if (zz != null)
            {
                zz.MenuId = taskResourceDto.MenuId;
                zz.ChangeFactor1 = taskResourceDto.ChangeFactor1;
                zz.ChangeFactor2 = taskResourceDto.ChangeFactor2;
                zz.CapWaste = taskResourceDto.CapWaste;
                zz.BaseCost = taskResourceDto.BaseCost;
                zz.Uncontrollable = taskResourceDto.Uncontrollable;
                zz.Formulas = taskResourceDto.Formulas;
                zz.CapRole = taskResourceDto.CapRole;
                await db.SaveChangesAsync(ct);
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateResourceAppStorageTenantAsync(int TenantId, int ResourceId, ResourceTenantLinkBase taskResourceDto, CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var zz = await db.ResourceTenant.FirstOrDefaultAsync
                (x => x.TenantId == TenantId && x.ResourceId == ResourceId, ct);
            if(zz == null)
            {
                zz = new Entities.Assignments.ResourceTenantLinkEntity
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
                db.ResourceTenant.Add(zz);
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
