using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.UnitGroups
{
    public class UnitGroupsService(IDbContextFactory<TaskResourceBlueprintsContext> ContextFactory) : IUnitGroupsService
    {
        public async Task<int> AddAsync(TaskUnitGroup obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            context.TaskUnitGroups.Add(obj);
            await context.SaveChangesAsync();
            return obj.Id;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.TaskUnitGroups.FindAsync(id);
            if (entity == null)
            {
                return false;
            }
            context.TaskUnitGroups.Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<TaskUnitGroup> GetByIdAsync(int id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.TaskUnitGroups.FindAsync(id);
        }

        public async Task<List<TaskUnitGroup>> GetAllAsync()
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            return await context.TaskUnitGroups.ToListAsync();
        }

        public async Task<bool> UpdateAsync(TaskUnitGroup obj)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.TaskUnitGroups.FindAsync(obj.Id);
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
