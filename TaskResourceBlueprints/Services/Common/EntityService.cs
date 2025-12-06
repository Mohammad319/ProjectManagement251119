using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Common
{
    public sealed class EntityService<T> : DbContextServiceBase, IEntityService<T>
        where T : class
    {
        public EntityService(IDbContextFactory<TaskResourceBlueprintsContext> contextFactory)
            : base(contextFactory)
        {
        }

        public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await CreateDbContextAsync(cancellationToken);
            return await context.Set<T>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            await using var context = await CreateDbContextAsync(cancellationToken);
            return await context.Set<T>().FindAsync([id], cancellationToken);
        }

        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await using var context = await CreateDbContextAsync(cancellationToken);
            context.Set<T>().Add(entity);
            await context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            await using var context = await CreateDbContextAsync(cancellationToken);
            context.Set<T>().Update(entity);
            var changes = await context.SaveChangesAsync(cancellationToken);
            return changes > 0;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            await using var context = await CreateDbContextAsync(cancellationToken);
            var existing = await context.Set<T>().FindAsync([id], cancellationToken);
            if (existing is null)
                return false;

            context.Set<T>().Remove(existing);
            var changes = await context.SaveChangesAsync(cancellationToken);
            return changes > 0;
        }
    }
}
