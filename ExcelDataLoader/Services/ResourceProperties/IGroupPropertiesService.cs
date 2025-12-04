using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Entities;
using ProjectImportHub.Infrastructure;

namespace ProjectImportHub.Services.ResourceProperties
{
    public interface IGroupPropertiesService
    {
        Task<List<ResourcePropertySetEntity>> GetAllAsync();
        Task<ResourcePropertySetEntity> GetByIdAsync(int id);
        Task<bool> UpdateAsync(ResourcePropertySetEntity obj);
        Task<int> AddAsync(ResourcePropertySetEntity obj);
        Task<bool> DeleteAsync(int id);
    }
    public class GroupPropertiesService(IDbContextFactory<ProjectImportHubContext> ContextFactory) : IGroupPropertiesService
    {
        public async Task<int> AddAsync(ResourcePropertySetEntity obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            context.ResourcePropertySets.Add(obj);
            await context.SaveChangesAsync();
            return obj.Id;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.ResourcePropertySets.FindAsync(id);
            if (entity == null)
            {
                return false;
            }
            context.ResourcePropertySets.Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ResourcePropertySetEntity>> GetAllAsync()
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.ResourcePropertySets.ToListAsync();
        }

        public async Task<ResourcePropertySetEntity> GetByIdAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.ResourcePropertySets.FindAsync(id);
        }

        public async Task<bool> UpdateAsync(ResourcePropertySetEntity obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.ResourcePropertySets.FindAsync(obj.Id);
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
