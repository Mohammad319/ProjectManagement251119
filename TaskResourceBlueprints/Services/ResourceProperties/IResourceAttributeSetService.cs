using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.ResourceProperties
{
    public interface IResourceAttributeSetService
    {
        Task<List<ResourceAttributeSet>> GetAllAsync(CancellationToken ct = default);
        Task<bool> UpdateAsync(ResourceAttributeSet attributeSet, CancellationToken ct = default);
        Task<int> CreateAsync(ResourceAttributeSet attributeSet, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }

    public class ResourceAttributeSetService(IDbContextFactory<TaskResourceBlueprintsContext> contextFactory) : IResourceAttributeSetService
    {
        public async Task<int> CreateAsync(ResourceAttributeSet attributeSet, CancellationToken ct = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);

            await context.ResourceAttributeSets.AddAsync(attributeSet, ct);
            await context.SaveChangesAsync(ct);

            return attributeSet.Id;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);

            var entity = await context.ResourceAttributeSets.FindAsync([id], ct);
            if (entity is null)
                return false;

            context.ResourceAttributeSets.Remove(entity);
            await context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<List<ResourceAttributeSet>> GetAllAsync(CancellationToken ct = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);

            return await context.ResourceAttributeSets
                .AsNoTracking()
                .OrderBy(x => x.DisplayName)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
        }

        public async Task<bool> UpdateAsync(ResourceAttributeSet attributeSet, CancellationToken ct = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);

            var entity = await context.ResourceAttributeSets.FindAsync([attributeSet.Id], ct);
            if (entity is null)
                return false;

            context.Entry(entity).CurrentValues.SetValues(attributeSet);
            await context.SaveChangesAsync(ct);

            return true;
        }
    }
}
