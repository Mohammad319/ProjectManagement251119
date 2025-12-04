using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Entities;
using ProjectImportHub.Infrastructure;

namespace ProjectImportHub.Services.Resource
{
    public interface IResourceFolderService
    {
        Task<List<ResourceFolderEntity>> GetAllAsync();
        Task<List<Entities.ResourceEntity>> GetAllChildFolderIds(HashSet<int> selectedFolderIds);
        Task<List<ResourceEntity>> UpdateResourcesAsync(HashSet<int> selectedFolderIds);
        Task<ResourceFolderEntity> GetByIdAsync(int id);
        Task<bool> UpdateAsync(ResourceFolderEntity obj);
        Task<int> AddAsync(ResourceFolderEntity obj);
        Task<bool> DeleteAsync(int id);
    }
    public class ResourceFolderService(IDbContextFactory<ProjectImportHubContext> ContextFactory) : IResourceFolderService
    {
        public async Task<int> AddAsync(ResourceFolderEntity obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            context.Folders.Add(obj);
            await context.SaveChangesAsync();
            return obj.Id;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.Folders.FindAsync(id);
            if (entity == null)
            {
                return false;
            }
            context.Folders.Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }
        public async Task<List<ResourceFolderEntity>> GetAllAsync()
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.Folders.OrderBy(f => f.SortOrder).ToListAsync();
        }
        public async Task<List<Entities.ResourceEntity>> UpdateResourcesAsync(HashSet<int> selectedFolderIds)
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
                .Where(r => r.FolderId != null && allIds.Contains(r.FolderId.Value))
                .ToListAsync();
        }
        public async Task<List<Entities.ResourceEntity>> GetAllChildFolderIds(HashSet<int> selectedFolderIds)
        {
            List<Entities.ResourceEntity> resourcesToShow = [];
            using var db = ContextFactory.CreateDbContext();
            foreach (var id in selectedFolderIds)
            {
                var allChildIds = await GetAllChildFolderIds(db, id);
                allChildIds.Add(id);

                var resources = await db.Resources
                                        .Where(r => r.FolderId != null && allChildIds.Contains(r.FolderId.Value))
                                        .ToListAsync();
                resourcesToShow.AddRange(resources);
            }
            return resourcesToShow;
        }
        private async Task<List<int>> GetAllChildFolderIds(ProjectImportHubContext db, int parentId)
        {
            var childIds = await db.Folders.Where(f => f.ParentFolderId == parentId)
                                   .Select(f => f.Id).ToListAsync();
            var allIds = new List<int>(childIds);
            foreach (var id in childIds)
            {
                allIds.AddRange(await GetAllChildFolderIds(db, id));
            }
            return allIds;
        }
        public async Task<ResourceFolderEntity> GetByIdAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.Folders.FindAsync(id);
        }
        public async Task<bool> UpdateAsync(ResourceFolderEntity obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.Folders.FindAsync(obj.Id);
            if (entity == null)
            {
                return false;
            }
            context.Entry(entity).CurrentValues.SetValues(obj);
            await context.SaveChangesAsync();
            return true;
        }

    }
}
