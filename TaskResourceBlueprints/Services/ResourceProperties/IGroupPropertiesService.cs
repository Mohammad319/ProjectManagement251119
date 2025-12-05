using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.ResourceProperties
{
    public interface IGroupPropertiesService
    {
        Task<List<ResourceAttributeSet>> GetAllAsync();
        Task<ResourceAttributeSet> GetByIdAsync(int id);
        Task<bool> UpdateAsync(ResourceAttributeSet obj);
        Task<int> AddAsync(ResourceAttributeSet obj);
        Task<bool> DeleteAsync(int id);
    }
    public class GroupPropertiesService(IDbContextFactory<TaskResourceBlueprintsContext> ContextFactory) : IGroupPropertiesService
    {
        public async Task<int> AddAsync(ResourceAttributeSet obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            context.ResourceAttributeSets.Add(obj);
            await context.SaveChangesAsync();
            return obj.Id;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.ResourceAttributeSets.FindAsync(id);
            if (entity == null)
            {
                return false;
            }
            context.ResourceAttributeSets.Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ResourceAttributeSet>> GetAllAsync()
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.ResourceAttributeSets.ToListAsync();
        }

        public async Task<ResourceAttributeSet> GetByIdAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.ResourceAttributeSets.FindAsync(id);
        }

        public async Task<bool> UpdateAsync(ResourceAttributeSet obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.ResourceAttributeSets.FindAsync(obj.Id);
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
