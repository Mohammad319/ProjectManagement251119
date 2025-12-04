using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Infrastructure;
using ProjectImportHub.Services;

namespace ExcelDataLoader.Services
{
    public abstract class BaseService(IDbContextFactory<ProjectImportHubContext> contextFactory)
    {
        protected readonly IDbContextFactory<ProjectImportHubContext> _contextFactory = contextFactory;

        protected ProjectImportHubContext CreateContext() => _contextFactory.CreateDbContext();
    }

    public class EntityService<T>(IDbContextFactory<ProjectImportHubContext> contextFactory) : BaseService(contextFactory),IEntityService<T> where T : class
    {
        public async Task<List<T>> GetAllAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Set<T>().AsNoTracking().ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Set<T>().FindAsync(id);
        }

        public async Task<T> AddAsync(T entity)
        {
            using var context = _contextFactory.CreateDbContext();
            await context.Set<T>().AddAsync(entity);
            await context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(T entity)
        {
            using var context = _contextFactory.CreateDbContext();
            var idProperty = entity.GetType().GetProperty("Id");
            if (idProperty == null)
                throw new Exception("No Id property found on entity");

            var id = (int)idProperty.GetValue(entity)!;
            var existing = await context.Set<T>().FindAsync(id);
            if (existing == null)
                return false;

            context.Entry(existing).CurrentValues.SetValues(entity);
            await context.SaveChangesAsync();
            return true;

        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            var existing = await context.Set<T>().FindAsync(id);
            if (existing == null)
                return false;

            context.Set<T>().Remove(existing);
            await context.SaveChangesAsync();
            return true;
        }
    }

}
