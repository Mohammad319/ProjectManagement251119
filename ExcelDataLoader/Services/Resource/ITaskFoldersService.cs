using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Dto.ProjectTask;
using TaskResourceBlueprints.Dto.Resource;
using TaskResourceBlueprints.Entities.Questions.Assignments;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Resource
{
    public interface ITaskFoldersService
    {
        Task<IReadOnlyList<FolderDto>> GetVisibleFoldersAsync(int projectTaskId, CancellationToken ct = default);

        Task<IReadOnlyList<FolderDto>> GetVisibleFoldersAsync(
            int projectTaskId, IReadOnlyList<int> visibleFolderIds, CancellationToken ct = default);

        Task<(IReadOnlyList<ResourceRowDto> rows, string folderName)> GetFolderResourcesAsync(
            int projectTaskId, int folderId, CancellationToken ct = default);

        Task AddAssignmentAsync(int projectTaskId, int resourceId, CancellationToken ct = default);
        Task RemoveAssignmentAsync(int projectTaskId, int resourceId, CancellationToken ct = default);
        Task<bool> RemoveAssignmentAsync(int id, CancellationToken ct = default);
    }

    public sealed class TaskFoldersService(IDbContextFactory<TaskResourceBlueprintsContext> factory)
        : ITaskFoldersService
    {
        public async Task<IReadOnlyList<FolderDto>> GetVisibleFoldersAsync(int projectTaskId, CancellationToken ct = default)
        {
            await using var db = await factory.CreateDbContextAsync(ct);

            var ids = await db.Tasks.AsNoTracking()
                .Where(t => t.Id == projectTaskId)
                .Select(t => t.VisibleFolderIds)
                .FirstOrDefaultAsync(ct) ?? [];

            return await GetVisibleFoldersAsync(projectTaskId, ids, ct);
        }

        public async Task<IReadOnlyList<FolderDto>> GetVisibleFoldersAsync(
            int projectTaskId, IReadOnlyList<int> visibleFolderIds, CancellationToken ct = default)
        {
            if (visibleFolderIds is null || visibleFolderIds.Count == 0)
                return Array.Empty<FolderDto>();

            await using var db = await factory.CreateDbContextAsync(ct);

            return await db.ResourceCategories.AsNoTracking()
                .Where(f => visibleFolderIds.Contains(f.Id))
                .OrderBy(f => f.SortOrder)
                .Select(f => new FolderDto(f.Id, f.DisplayName))
                .ToListAsync(ct);
        }

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
                    r.Data.ChangeFactor2,     // ✔️ انتبه: CF2 الصحيح
                    r.Data.CapWaste,
                    r.Data.Cost,
                    r.Data.BaseCost,
                    assignedIds.Contains(r.Id)
                ))
                .ToListAsync(ct);

            return (rows, folderName);
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
                ChangeFactor2 = r.ChangeFactor2,   // ✔️ كان خطأ عندك سابقًا
                CapWaste = r.CapWaste,
                BaseCost = r.BaseCost,
                //ac = true
            });

            await db.SaveChangesAsync(ct);
        }

        public async Task RemoveAssignmentAsync(int projectTaskId, int resourceId, CancellationToken ct = default)
        {
            await using var db = await factory.CreateDbContextAsync(ct);

            var ite = await db.TaskResourceAssignments
                .FirstOrDefaultAsync(x => x.TaskId == projectTaskId && x.ResourceId == resourceId, ct);
            if (ite is null) return;

            db.TaskResourceAssignments.Remove(ite);
            await db.SaveChangesAsync(ct);
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
