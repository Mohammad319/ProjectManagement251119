using Application.Extention;
using Application.Services.CalculationItems.Storage;
using Domain.Entities.Calculation;
using Persistence.Factory;
using System.Text.Json;

namespace Persistence.Service.CalculationItems.Storage
{
    public sealed class StorageCommandService(IDbContextFactoryTenant dbFactory) : IStorageCommandService
    {
        public async Task<bool> CreateAsync(
            CalculationItemType type,
            AuthorityStorage level,
            StorageSort sort,
            int id,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            object? obj = null;
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (type == CalculationItemType.task)
            {

                // جلب التسك مع الأبناء والموارد
                var tasks = await context.Tasks
                    .FromSqlRaw("EXEC GetRecursiveTasks {0}", id)
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .ToListAsync(ct);

                if (tasks is null) return false;

                var taskIds = tasks.Select(t => t.Id).ToList();

                var resources = await context.Resources
                    .Where(r => taskIds.Contains(r.TaskId))
                    .AsNoTracking()
                    .ToListAsync(ct);

                foreach (var t in tasks)
                    t.Resources = resources.Where(r => r.TaskId == t.Id).ToList();

                TaskExtention.BuildTaskHierarchy(tasks);

                var root = tasks.First(x => x.Id == id);
                obj = TaskExtention.Reset(root);
            }
            else if (type == CalculationItemType.resource)
            {
                obj = await context.Resources
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new
                    {
                        x.ResourceTypeId,
                        x.Name,
                        x.AccountId,
                        x.IsActive,
                        x.ResType,
                        x.StatusId,
                        x.ResourceSortId,
                        x.Metadata
                    })
                    .FirstOrDefaultAsync(ct);
            }

            if (obj is null)
                return false;

            var st = new StorageEntity(
                JsonSerializer.Serialize(obj),
                type,
                sort,
                level,
                departmentId ?? 0,
                userId);

            context.Storages.Add(st);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var st = await context.Storages.FindAsync(id);
            if (st == null) return false;

            context.Storages.Remove(st);
            await context.SaveChangesAsync(ct);
            return true;
        }
    }

}
