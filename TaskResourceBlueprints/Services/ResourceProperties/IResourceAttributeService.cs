using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.ResourceProperties
{
    public interface IResourceAttributeService
    {
        Task<List<ResourceAttribute>> GetByAttributeSetIdAsync(int groupId, CancellationToken ct = default);
        Task<bool> UpdateAsync(ResourceAttribute attribute, CancellationToken ct = default);
        Task<int> CreateAsync(ResourceAttribute attribute, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }

    public class ResourceAttributeService(IDbContextFactory<TaskResourceBlueprintsContext> contextFactory) : IResourceAttributeService
    {
        public async Task<int> CreateAsync(ResourceAttribute attribute, CancellationToken ct = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);

            await context.ResourceAttributes.AddAsync(attribute, ct);
            await context.SaveChangesAsync(ct);

            return attribute.Id;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);

            var entity = await context.ResourceAttributes.FindAsync(id);
            if (entity is null)
                return false;

            context.ResourceAttributes.Remove(entity);
            await context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<List<ResourceAttribute>> GetByAttributeSetIdAsync(int groupId, CancellationToken ct = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);

            return await context.ResourceAttributes
                .AsNoTracking()
                .Where(x => x.AttributeSetId == groupId)
                .OrderBy(x => x.Id) // لو عندك SortOrder استخدمه هنا بدل Id
                .ToListAsync(ct);
        }

        public async Task<bool> UpdateAsync(ResourceAttribute attribute, CancellationToken ct = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);

            var entity = await context.ResourceAttributes.FindAsync(attribute.Id);
            if (entity is null)
                return false;

            context.Entry(entity).CurrentValues.SetValues(attribute);
            await context.SaveChangesAsync(ct);

            return true;
        }
    }
}
