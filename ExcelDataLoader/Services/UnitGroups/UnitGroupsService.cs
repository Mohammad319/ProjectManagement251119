using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Entities;
using ProjectImportHub.Infrastructure;

namespace ProjectImportHub.Services.UnitGroups
{
    public class UnitGroupsService(IDbContextFactory<ProjectImportHubContext> ContextFactory) : IUnitGroupsService
    {
        public async Task<int> AddAsync(UnitGroupEntity obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            context.UnitGroups.Add(obj);
            await context.SaveChangesAsync();
            return obj.Id;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.UnitGroups.FindAsync(id);
            if (entity == null)
            {
                return false;
            }
            context.UnitGroups.Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<UnitGroupEntity> GetByIdAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.UnitGroups.FindAsync(id);
        }

        public async Task<List<UnitGroupEntity>> GetAllAsync()
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.UnitGroups.ToListAsync();
        }

        public async Task<bool> UpdateAsync(UnitGroupEntity obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.UnitGroups.FindAsync(obj.Id);
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
