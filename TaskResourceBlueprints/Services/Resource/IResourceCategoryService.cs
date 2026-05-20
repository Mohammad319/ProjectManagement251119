using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Dto.ProjectTask;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Resource
{
    public interface IResourceCategoryService
    {
        Task<IReadOnlyList<FolderDto>> GetVisibleFoldersAsync(int taskId, CancellationToken ct = default);
        Task<List<ResourceCategory>> GetAllAsync();
        Task<List<ResourceDefinition>> GetResourcesAsync(HashSet<int> selectedFolderIds);
        Task<IReadOnlyList<ResourceDefinition>> GetFolderResourcesAsync(int folderId, CancellationToken ct);

        Task<ResourceCategory?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(ResourceCategory obj);
        Task<int> AddAsync(ResourceCategory obj);
        Task<bool> DeleteAsync(int id);
    }

    public class ResourceCategoryService(IDbContextFactory<TaskResourceBlueprintsContext> ContextFactory) : IResourceCategoryService
    {
        public async Task<IReadOnlyList<FolderDto>> GetVisibleFoldersAsync(int taskId, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);

            var ids = await db.Tasks.AsNoTracking()
                .Where(t => t.Id == taskId)
                .Select(t => t.VisibleFolderIds)
                .FirstOrDefaultAsync(ct) ?? [];

            return await db.ResourceCategories.AsNoTracking()
                .Where(f => ids.Contains(f.Id))
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Id)
                .Select(f => new FolderDto(f.Id, f.DisplayName))
                .ToListAsync(ct);
        }

        public async Task<int> AddAsync(ResourceCategory obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            context.ResourceCategories.Add(obj);
            await context.SaveChangesAsync();
            return obj.Id;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.ResourceCategories.FindAsync(id);
            if (entity == null)
            {
                return false;
            }

            context.ResourceCategories.Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ResourceCategory>> GetAllAsync()
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.ResourceCategories
                .AsNoTracking()
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Id)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ResourceDefinition>> GetFolderResourcesAsync(int folderId, CancellationToken ct)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);

            return await db.Resources
                .AsNoTracking()
                .Where(r => r.FolderId == folderId)
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.Name)
                .ToListAsync(ct);
        }

        public async Task<List<ResourceDefinition>> GetResourcesAsync(HashSet<int> selectedFolderIds)
        {
            using var db = ContextFactory.CreateDbContext();

            var allIds = new HashSet<int>();
            foreach (var id in selectedFolderIds)
            {
                var child = await GetAllChildFolderIds(db, id);
                allIds.UnionWith(child);
                allIds.Add(id);
            }

            return await db.Resources
                .AsNoTracking()
                .Where(r => r.FolderId != null && allIds.Contains(r.FolderId.Value))
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.Name)
                .ToListAsync();
        }

        private async Task<List<int>> GetAllChildFolderIds(TaskResourceBlueprintsContext db, int parentId)
        {
            var childIds = await db.ResourceCategories
                .Where(f => f.ParentCategoryId == parentId)
                .Select(f => f.Id)
                .ToListAsync();

            var allIds = new List<int>(childIds);
            foreach (var id in childIds)
            {
                allIds.AddRange(await GetAllChildFolderIds(db, id));
            }

            return allIds;
        }

        public async Task<ResourceCategory?> GetByIdAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.ResourceCategories.FindAsync(id);
        }

        public async Task<bool> UpdateAsync(ResourceCategory obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.ResourceCategories.FindAsync(obj.Id);
            if (entity == null) return false;

            if (obj.ParentCategoryId.HasValue && await WouldCreateCycleAsync(context, obj.Id, obj.ParentCategoryId.Value))
                return false;

            context.Entry(entity).CurrentValues.SetValues(obj);
            await context.SaveChangesAsync();
            return true;
        }

        private static async Task<bool> WouldCreateCycleAsync(TaskResourceBlueprintsContext db, int id, int newParentId)
        {
            var current = (int?)newParentId;
            while (current.HasValue)
            {
                if (current == id) return true;
                current = await db.ResourceCategories
                    .Where(x => x.Id == current.Value)
                    .Select(x => (int?)x.ParentCategoryId)
                    .FirstOrDefaultAsync();
            }
            return false;
        }
    }
}
