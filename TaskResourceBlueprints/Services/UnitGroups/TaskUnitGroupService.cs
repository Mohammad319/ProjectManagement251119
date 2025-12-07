using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.UnitGroups
{
    public class TaskUnitGroupService(IDbContextFactory<TaskResourceBlueprintsContext> contextFactory) : ITaskUnitGroupService
    {
        public async Task<int> AddAsync(TaskUnitGroup unitGroup, CancellationToken ct = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);
            await context.TaskUnitGroups.AddAsync(unitGroup, ct);
            await context.SaveChangesAsync(ct);
            return unitGroup.Id;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);

            var entity = await context.TaskUnitGroups.FindAsync([id], ct);
            if (entity is null)
                return false;

            context.TaskUnitGroups.Remove(entity);
            await context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<TaskUnitGroup?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);

            return await context.TaskUnitGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<List<TaskUnitGroup>> GetAllAsync(CancellationToken ct = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);

            return await context.TaskUnitGroups
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<bool> UpdateAsync(TaskUnitGroup unitGroup, CancellationToken ct = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);

            var entity = await context.TaskUnitGroups.FindAsync([unitGroup.Id], ct);
            if (entity is null)
                return false;

            context.Entry(entity).CurrentValues.SetValues(unitGroup);
            await context.SaveChangesAsync(ct);

            return true;
        }
    }

}
