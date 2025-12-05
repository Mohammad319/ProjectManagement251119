using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Entities;
using ProjectImportHub.Infrastructure;

namespace ProjectImportHub.Services.Resource
{
    public interface IResourceFolderService
    {
        Task<List<ResourceCategory>> GetAllAsync();
        Task<List<Entities.ResourceDefinition>> GetAllChildFolderIds(HashSet<int> selectedFolderIds);
        Task<List<ResourceDefinition>> UpdateResourcesAsync(HashSet<int> selectedFolderIds);
        Task<ResourceCategory> GetByIdAsync(int id);
        Task<bool> UpdateAsync(ResourceCategory obj);
        Task<int> AddAsync(ResourceCategory obj);
        Task<bool> DeleteAsync(int id);
    }
    public class ResourceFolderService(IDbContextFactory<ProjectImportHubContext> ContextFactory) : IResourceFolderService
    {
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
            return await context.ResourceCategories.OrderBy(f => f.SortOrder).ToListAsync();
        }
        public async Task<List<Entities.ResourceDefinition>> UpdateResourcesAsync(HashSet<int> selectedFolderIds)
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
        public async Task<List<Entities.ResourceDefinition>> GetAllChildFolderIds(HashSet<int> selectedFolderIds)
        {
            List<Entities.ResourceDefinition> resourcesToShow = [];
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
            var childIds = await db.ResourceCategories.Where(f => f.ParentCategoryId == parentId)
                                   .Select(f => f.Id).ToListAsync();
            var allIds = new List<int>(childIds);
            foreach (var id in childIds)
            {
                allIds.AddRange(await GetAllChildFolderIds(db, id));
            }
            return allIds;
        }
        public async Task<ResourceCategory> GetByIdAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.ResourceCategories.FindAsync(id);
        }
        public async Task<bool> UpdateAsync(ResourceCategory obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.ResourceCategories.FindAsync(obj.Id);
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
