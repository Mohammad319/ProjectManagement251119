using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Entities;
using ProjectImportHub.Infrastructure;

namespace ProjectImportHub.Services.ResourceProperties
{
    public interface IResourcePropertiesService
    {
        Task<List<ResourcePropertyEntity>> GetByGroupIdAsync(int groupId);
        Task<bool> UpdateAsync(ResourcePropertyEntity obj);
        Task<int> AddAsync(ResourcePropertyEntity obj);
        Task<bool> DeleteAsync(int id);
    }
    public class ResourcePropertiesService(IDbContextFactory<ProjectImportHubContext> ContextFactory) : IResourcePropertiesService
    {
        public async Task<int> AddAsync(ResourcePropertyEntity obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            context.ResourceProperties.Add(obj);
            await context.SaveChangesAsync();
            return obj.Id;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.ResourceProperties.FindAsync(id);
            if (entity == null)
            {
                return false;
            }
            context.ResourceProperties.Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ResourcePropertyEntity>> GetByGroupIdAsync(int groupId)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.ResourceProperties.Where(x => x.PropertySetId == groupId).ToListAsync();
        }

        public async Task<bool> UpdateAsync(ResourcePropertyEntity obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.ResourceProperties.FindAsync(obj.Id);
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
