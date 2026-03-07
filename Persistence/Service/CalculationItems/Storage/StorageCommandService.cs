using Application.Extention;
using Application.Services.CalculationItems.Storage;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using Persistence.Service.Sql;
using System.Text.Json;

namespace Persistence.Service.CalculationItems.Storage
{
    public sealed class StorageCommandService(IDbContextFactoryTenant dbFactory) : IStorageCommandService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task<bool> CreateAsync(
            CalculationItemType type,
            AuthorityStorage level,
            StorageSort sort,
            int id,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (departmentId is > 0)
            {
                var departmentExists = await context.Department
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == departmentId.Value, ct);

                if (!departmentExists)
                    return false;
            }

            object? obj = null;

            if (type == CalculationItemType.task)
            {
                var calcId = await context.Tasks
                    .AsNoTracking()
                    .Where(t => t.Id == id)
                    .Select(t => t.CalculationId)
                    .FirstOrDefaultAsync(ct);

                if (calcId <= 0)
                    return false;

                var tasks = await RecursiveTasksCte.Query(context, id, calcId)
                    .AsNoTracking()
                    .ToListAsync(ct);

                if (tasks.Count == 0)
                    return false;

                var taskIds = tasks.Select(t => t.Id).ToList();

                var resources = await context.Resources
                    .Where(r => taskIds.Contains(r.TaskId))
                    .AsNoTracking()
                    .ToListAsync(ct);

                var resourcesByTaskId = resources
                    .GroupBy(r => r.TaskId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var t in tasks)
                    t.Resources = resourcesByTaskId.TryGetValue(t.Id, out var list) ? list : [];

                TaskExtention.BuildTaskHierarchy(tasks);

                var root = tasks.First(t => t.Id == id);
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
                JsonSerializer.Serialize(obj, JsonOptions),
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

            var deleted = await context.Storages
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(ct);

            return deleted > 0;
        }
    }
}
