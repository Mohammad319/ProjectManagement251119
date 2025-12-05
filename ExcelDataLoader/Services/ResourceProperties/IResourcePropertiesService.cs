using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Entities.Resources;
using ProjectImportHub.Infrastructure;

namespace ProjectImportHub.Services.ResourceProperties
{
    public interface IResourcePropertiesService
    {
        Task<List<ResourceAttribute>> GetByGroupIdAsync(int groupId);
        Task<bool> UpdateAsync(ResourceAttribute obj);
        Task<int> AddAsync(ResourceAttribute obj);
        Task<bool> DeleteAsync(int id);
    }
    public class ResourcePropertiesService(IDbContextFactory<ProjectImportHubContext> ContextFactory) : IResourcePropertiesService
    {
        public async Task<int> AddAsync(ResourceAttribute obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            context.ResourceAttributes.Add(obj);
            await context.SaveChangesAsync();
            return obj.Id;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.ResourceAttributes.FindAsync(id);
            if (entity == null)
            {
                return false;
            }
            context.ResourceAttributes.Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ResourceAttribute>> GetByGroupIdAsync(int groupId)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.ResourceAttributes.Where(x => x.AttributeSetId == groupId).ToListAsync();
        }

        public async Task<bool> UpdateAsync(ResourceAttribute obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.ResourceAttributes.FindAsync(obj.Id);
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
