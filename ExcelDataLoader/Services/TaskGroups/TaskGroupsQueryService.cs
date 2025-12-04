using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Entities;
using ProjectImportHub.Infrastructure;

namespace ProjectImportHub.Services.TaskGroups;
public interface ITaskGroupsQueryService
{
    Task<ProjectTaskEntity?> GetTaskGraphAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<ResourceFolderEntity>> GetVisibleFoldersAsync(int taskId, CancellationToken ct);
    Task<IReadOnlyList<ResourceEntity>> GetFolderResourcesAsync(int folderId, CancellationToken ct);
}
public sealed class TaskGroupsQueryService(IDbContextFactory<ProjectImportHubContext> _factory) : ITaskGroupsQueryService
{
    public async Task<ProjectTaskEntity?> GetTaskGraphAsync(int id, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Tasks
            .AsNoTracking()
            .Include(t => t.OptionGroups).ThenInclude(g => g.Options)
            .Include(t => t.ResourceOptionGroups).ThenInclude(g => g.Items).ThenInclude(i => i.Resource)
            .Include(t => t.NumericInputs)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<IReadOnlyList<ResourceFolderEntity>> GetVisibleFoldersAsync(int taskId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var task = await db.Tasks.AsNoTracking()
            .Select(t => new { t.Id, t.VisibleFolderIds })
            .FirstAsync(t => t.Id == taskId, ct);

        var ids = task.VisibleFolderIds ?? [];
        var folders = await db.Folders
            .AsNoTracking()
            .Where(f => ids.Contains(f.Id))
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);

        return folders;
    }

    public async Task<IReadOnlyList<ResourceEntity>> GetFolderResourcesAsync(int folderId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Resources
            .AsNoTracking()
            .Where(r => r.FolderId == folderId)
            .ToListAsync(ct);
    }
}
