using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Enums;
using TaskResourceBlueprints.Dto.Resource;
using TaskResourceBlueprints.Entities.Questions.Assignments;
using TaskResourceBlueprints.Infrastructure;
using TaskResourceBlueprints.Services.ProjectTask;

namespace TaskResourceBlueprints.Services.Resource
{
    public interface ITaskResourceService
    {
        Task<(IReadOnlyList<ResourceRowDto> rows, string folderName)> GetFolderResourcesAsync(
            int projectTaskId, int folderId, CancellationToken ct = default);
        Task<TaskResourceDto?> GetTaskResourceAsync(int TaskId, int ResourceId, CancellationToken ct);

        Task<bool> UpdateAssignmentAsync(int TaskId, int ResourceId, TaskResourceDto taskResourceDto, CancellationToken ct);
        Task<bool> AddAssignmentAsync(TaskResourceAssignment assignment, CancellationToken cancellationToken = default);
        Task AddAssignmentAsync(int projectTaskId, int resourceId, CancellationToken ct = default);
        Task RemoveResourceFromTaskAsync(int projectTaskId, int resourceId, CancellationToken ct = default);
        Task<bool> RemoveAssignmentAsync(int id, CancellationToken ct = default);
    }

    public sealed class TaskResourceService(IDbContextFactory<TaskResourceBlueprintsContext> factory)
        : ITaskResourceService
    {

        public async Task<(IReadOnlyList<ResourceRowDto> rows, string folderName)> GetFolderResourcesAsync(
            int projectTaskId, int folderId, CancellationToken ct = default)
        {
            await using var db = await factory.CreateDbContextAsync(ct);

            var assignedIds = await db.TaskResourceAssignments.AsNoTracking()
                .Where(tr => tr.TaskId == projectTaskId)
                .Select(tr => tr.ResourceId)
                .ToListAsync(ct);

            var folderName = await db.Resources.AsNoTracking()
                .Where(f => f.Id == folderId)
                .Select(f => f.Name)
                .FirstOrDefaultAsync(ct) ?? string.Empty;

            var rows = await db.Resources.AsNoTracking()
                .Where(r => r.FolderId == folderId)
                .Select(r => new ResourceRowDto(
                    r.Id,
                    r.Name,
                    r.ResType.ToString(),
                    r.Data.Unit,
                    r.Data.Quantity,
                    r.Data.ChangeFactor1,
                    r.Data.ChangeFactor2,
                    r.Data.CapWaste,
                    r.Data.Cost,
                    r.Data.BaseCost,
                    assignedIds.Contains(r.Id)
                ))
                .ToListAsync(ct);

            return (rows, folderName);
        }
        public async Task<TaskResourceDto?> GetTaskResourceAsync(int taskId, int resourceId, CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);

            var zz = await db.TaskResourceAssignments.AsNoTracking()
                .Where(x => x.TaskId == taskId && x.ResourceId == resourceId)
                .Select(x => new TaskResourceDto
                {
                    Active = x.Resource == null ? false : x.Resource.IsActive,
                    MenuId = x.MenuId,
                    BaseCost = (double?)x.BaseCost,
                    CapRole = x.CapacityRoles,
                    CapWaste = x.CapWaste,
                    ChangeFactor1 = x.ChangeFactor1,
                    ChangeFactor2 = x.ChangeFactor2,
                    Formulas = x.Expressions,
                    ResType = x.Resource == null ? ResourceTypesEnum.Adjustment : x.Resource.ResType,
                    Uncontrollable = x.Uncontrollable,
                })
                .FirstOrDefaultAsync(ct);

            return zz;
        }
        public async Task<bool> UpdateAssignmentAsync(int TaskId, int ResourceId, TaskResourceDto taskResourceDto, CancellationToken ct)
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
                zz.Expressions = taskResourceDto.Formulas;
                zz.CapacityRoles = taskResourceDto.CapRole;
                await db.SaveChangesAsync(ct);
                return true;
            }
            return false;
        }
        public async Task<bool> AddAssignmentAsync(
    TaskResourceAssignment assignment,
    CancellationToken cancellationToken = default)
        {
            await using var context = await factory.CreateDbContextAsync(cancellationToken);

            // يمكن التحقق من عدم التكرار إن احتجت:
            var exists = await context.TaskResourceAssignments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TaskId == assignment.TaskId &&
                    x.ResourceId == assignment.ResourceId,
                    cancellationToken);

            if (exists)
                return false;

            await context.TaskResourceAssignments.AddAsync(assignment, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        public async Task AddAssignmentAsync(int projectTaskId, int resourceId, CancellationToken ct = default)
        {
            await using var db = await factory.CreateDbContextAsync(ct);

            var exists = await db.TaskResourceAssignments
                .AnyAsync(x => x.TaskId == projectTaskId && x.ResourceId == resourceId, ct);
            if (exists) return;

            var r = await db.Resources.AsNoTracking()
                .Where(x => x.Id == resourceId)
                .Select(x => new
                {
                    x.Id,
                    x.Data.ChangeFactor1,
                    x.Data.ChangeFactor2,
                    x.Data.CapWaste,
                    x.Data.BaseCost
                })
                .FirstOrDefaultAsync(ct);

            if (r is null) return;

            db.TaskResourceAssignments.Add(new TaskResourceAssignment
            {
                TaskId = projectTaskId,
                ResourceId = r.Id,
                ChangeFactor1 = r.ChangeFactor1,
                ChangeFactor2 = r.ChangeFactor2,
                CapWaste = r.CapWaste,
                BaseCost = r.BaseCost,
            });

            await db.SaveChangesAsync(ct);
        }

        public async Task RemoveResourceFromTaskAsync(int taskId, int resourceId, CancellationToken ct = default)
        {
            await using var context = await factory.CreateDbContextAsync(ct);

            var existing = await context.TaskResourceAssignments
                .FirstOrDefaultAsync(
                    tr => tr.TaskId == taskId && tr.ResourceId == resourceId,
                    ct);

            if (existing is null)
                return;

            context.TaskResourceAssignments.Remove(existing);
            await context.SaveChangesAsync(ct);
        }

        public async Task<bool> RemoveAssignmentAsync(int id, CancellationToken ct = default)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var ite = await db.TaskResourceAssignments.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (ite is null) return false;

            db.TaskResourceAssignments.Remove(ite);
            await db.SaveChangesAsync(ct);
            return true;
        }
    }

}
