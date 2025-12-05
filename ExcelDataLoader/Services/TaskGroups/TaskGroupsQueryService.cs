using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.TaskGroups;
public interface ITaskGroupsQueryService
{
    Task<TaskDefinition?> GetTaskGraphAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<ResourceCategory>> GetVisibleFoldersAsync(int taskId, CancellationToken ct);
    Task<IReadOnlyList<ResourceDefinition>> GetFolderResourcesAsync(int folderId, CancellationToken ct);
}
public sealed class TaskGroupsQueryService(IDbContextFactory<TaskResourceBlueprintsContext> _factory) : ITaskGroupsQueryService
{
    public async Task<TaskDefinition?> GetTaskGraphAsync(int id, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Tasks
            .AsNoTracking()
            .Include(t => t.QuestionGroups).ThenInclude(g => g.Options)
            .Include(t => t.ResourceSelectors).ThenInclude(g => g.Items).ThenInclude(i => i.Resource)
            .Include(t => t.NumericQuestions)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<IReadOnlyList<ResourceCategory>> GetVisibleFoldersAsync(int taskId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var task = await db.Tasks.AsNoTracking()
            .Select(t => new { t.Id, t.VisibleFolderIds })
            .FirstAsync(t => t.Id == taskId, ct);

        var ids = task.VisibleFolderIds ?? [];
        var folders = await db.ResourceCategories
            .AsNoTracking()
            .Where(f => ids.Contains(f.Id))
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);

        return folders;
    }

    public async Task<IReadOnlyList<ResourceDefinition>> GetFolderResourcesAsync(int folderId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Resources
            .AsNoTracking()
            .Where(r => r.FolderId == folderId)
            .ToListAsync(ct);
    }
}
