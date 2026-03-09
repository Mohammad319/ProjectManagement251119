using Application.Extention;
using Application.Feature.Calculation.Resource;
using Application.Interfaces;
using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Exceptions;

namespace Persistence.Service.CalculationItems.Resource
{
    public class ResourceService(IDbContextFactoryTenant dbFactory, INotificationHub notification) : IResourceService
    {
        // -----------------------------------------------------
        // Copy
        // -----------------------------------------------------
        public async Task<bool> CopyAsync(
            IReadOnlyList<ResourceTaskItemDTO> items,
            int parentTaskId,
            int sourceCalcId,
            CancellationToken ct = default)
        {
            if (items is null || items.Count == 0) return true;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var parent = await context.Tasks
                .Where(x => x.Id == parentTaskId)
                .Select(x => new
                {
                    MaxOrder = x.Resources.Max(r => (int?)r.SortOrder),
                    NewCalcID = x.CalculationId,
                })
                .FirstOrDefaultAsync(ct);

            if (parent is null) return false;

            var ids = items.Select(x => x.Id).Distinct().ToList();

            // ✅ تحميل مرة واحدة بدل استعلام لكل عنصر
            var sourceResources = await context.Resources
                .AsNoTracking()
                .Where(r => ids.Contains(r.Id))
                .ToListAsync(ct);

            var valueById = items.ToDictionary(x => x.Id, x => x.Value);

            var entities = new List<ResourceEntity>(sourceResources.Count);

            int nextOrder = parent.MaxOrder.HasValue ? parent.MaxOrder.Value + 100 : 100;

            foreach (var source in sourceResources)
            {
                var sourceId = source.Id;
                var copy = ResourceExtention.Reset(source);

                if (sourceCalcId != parent.NewCalcID)
                {
                    copy.ClearCrossCalculationState(resetQuantityParam: true);
                    copy.Metadata.Quantity = valueById.TryGetValue(sourceId, out var v) ? v : copy.Metadata.Quantity;
                }
                else
                {
                    copy.PrimaryOfferId = null;
                }

                copy.MoveToTask(parentTaskId, nextOrder);
                nextOrder += 100;

                entities.Add(copy);
            }

            if (entities.Count == 0) return true;

            context.Resources.AddRange(entities);
            await context.SaveChangesAsync(ct);
            context.ChangeTracker.Clear();

            var newIds = entities.Select(x => x.Id).ToList();

            var loadedEntities = await context.Resources
                .Where(r => newIds.Contains(r.Id))
                .IncludeResourceLookups()
                .ToListAsync(ct);

            var resourceDtos = loadedEntities.Select(ResourceExtention.MapToResourceListDTO).ToList();

            await notification.SendNotificationAsync(
                parent.NewCalcID.ToString(),
                ObjectTypHub.resource,
                OperationType.AddRange,
                parentTaskId,
                resourceDtos);

            return true;
        }

        // -----------------------------------------------------
        // Create
        // -----------------------------------------------------
        public async Task<bool> CreateAsync(IReadOnlyList<ResourcePostDTO> items, int parentTaskId, CancellationToken ct = default)
        {
            if (items is null || items.Count == 0) return true;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var parent = await context.Tasks
                .Where(x => x.Id == parentTaskId)
                .Select(x => new
                {
                    MaxOrder = x.Resources.Max(r => (int?)r.SortOrder),
                    CalID = x.CalculationId,
                })
                .FirstOrDefaultAsync(ct);

            if (parent is null) return false;

            var entities = new List<ResourceEntity>(items.Count);
            int nextOrder = parent.MaxOrder.HasValue ? parent.MaxOrder.Value + 100 : 100;

            foreach (var dto in items)
            {
                var resource = ResourceEntity.Create(dto, nextOrder, parentTaskId);
                nextOrder += 100;
                if(resource.AccountId == 0)
                    resource.AccountId = null;
                entities.Add(resource);
            }

            context.Resources.AddRange(entities);
            await context.SaveChangesAsync(ct);
            context.ChangeTracker.Clear();
            var newIds = entities.Select(x => x.Id).ToList();

            var loadedEntities = await context.Resources
                .Where(r => newIds.Contains(r.Id))
                .IncludeResourceLookups()
                .ToListAsync(ct);

            var resourceDtos = loadedEntities.Select(ResourceExtention.MapToResourceListDTO).ToList();

            await notification.SendNotificationAsync(
                parent.CalID.ToString(),
                ObjectTypHub.resource,
                OperationType.AddRange,
                parentTaskId,
                resourceDtos);

            return true;
        }

        // -----------------------------------------------------
        // Cut (Move)
        // -----------------------------------------------------
        public async Task<bool> CutAsync(int taskId, int sourceCalcId, IReadOnlyList<ResourceTaskItemDTO> items, CancellationToken ct = default)
        {
            if (items is null || items.Count == 0) return true;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var parent = await context.Tasks
                .Where(x => x.Id == taskId)
                .Select(x => new
                {
                    CalID = x.CalculationId,
                    Max = x.Resources.Max(r => (int?)r.SortOrder),
                })
                .FirstOrDefaultAsync(ct);

            if (parent is null) return false;

            var ids = items.Select(x => x.Id).Distinct().ToList();

            // ✅ تحميل مرة واحدة بدل FindAsync داخل loop
            var resources = await context.Resources
                .Where(r => ids.Contains(r.Id))
                .ToListAsync(ct);

            var moved = new List<ResourceEntity>(resources.Count);

            foreach (var resource in resources)
            {
                if (resource.TaskId == taskId) continue;

                if (parent.CalID != sourceCalcId)
                {
                    if (!string.IsNullOrEmpty(resource.Metadata.QuantityParam))
                        resource.Metadata.QuantityParam = PMValuesConst.FixedQ;

                    resource.Offers = [];
                    resource.PrimaryOfferId = null;
                    resource.OpportunityId = null;
                }

                var nextOrder = parent.Max.HasValue ? parent.Max.Value + 100 : 100;
                resource.MoveToTask(taskId, nextOrder);
                parent = parent with { Max = resource.SortOrder }; // تحديث max للعنصر التالي

                moved.Add(resource);
            }

            if (moved.Count == 0) return true;

            context.Resources.UpdateRange(moved);
            await context.SaveChangesAsync(ct);

            if (sourceCalcId == parent.CalID)
            {
                await notification.SendNotificationAsync(
                    sourceCalcId.ToString(),
                    ObjectTypHub.resource,
                    OperationType.MoveRange,
                    Tuple.Create(taskId, moved.Select(x => x.Id)));
            }
            else
            {
                var movedLoaded = await context.Resources
                    .Where(r => moved.Select(x => x.Id).Contains(r.Id))
                    .IncludeResourceLookups()
                    .ToListAsync(ct);

                var listHub = movedLoaded.Select(x => x.MapToResourceListDTO()).ToList();

                await notification.SendNotificationAsync(
                    sourceCalcId.ToString(),
                    ObjectTypHub.resource,
                    OperationType.RemoveRange,
                    moved.Select(x => x.Id));

                await notification.SendNotificationAsync(
                    parent.CalID.ToString(),
                    ObjectTypHub.resource,
                    OperationType.AddRange,
                    taskId,
                    listHub);
            }

            return true;
        }

        // -----------------------------------------------------
        // Delete
        // -----------------------------------------------------
        public async Task<bool> DeleteAsync(IEnumerable<int> resourceIds, int calcId, CancellationToken ct = default)
        {
            var ids = resourceIds?.Where(id => id > 0).Distinct().ToList() ?? [];
            if (ids.Count == 0) return true;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            const int batchSize = 1000;
            var anyDeleted = false;

            for (int i = 0; i < ids.Count; i += batchSize)
            {
                var batch = ids.Skip(i).Take(batchSize).ToList();

                var affected = await context.Resources
                    .Where(x => batch.Contains(x.Id) && x.Task.CalculationId == calcId)
                    .ExecuteDeleteAsync(ct);

                anyDeleted |= affected > 0;
            }

            if (!anyDeleted) return true;

            await notification.SendNotificationAsync(
                calcId.ToString(),
                ObjectTypHub.resource,
                OperationType.RemoveRange,
                ids);

            return true;
        }



        // -----------------------------------------------------
        // New Order
        // -----------------------------------------------------
        public async Task<bool> NewOrderAsync(int id, int newOrder, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var resource = await context.Resources
                .Where(x => x.Id == id)
                .Select(x => new { Res = x, CalID = x.Task.CalculationId })
                .FirstOrDefaultAsync(ct);

            if (resource is null) return false;

            resource.Res.SetSortOrder(newOrder);
            await context.SaveChangesAsync(ct);

            var fullResource = await context.Resources
                .Where(x => x.Id == id)
                .Include(x => x.Offers)
                .IncludeResourceLookups()
                .FirstOrDefaultAsync(ct);

            if (fullResource is null) return false;

            await notification.SendNotificationAsync(
                resource.CalID.ToString(),
                ObjectTypHub.resource,
                OperationType.Update,
                fullResource.MapToResourceListDTO());

            return true;
        }

        // -----------------------------------------------------
        // Update
        // -----------------------------------------------------
        public async Task<bool> UpdateAsync(int resourceId, ResourcePostDTO res, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.Resources
                .Include(x => x.Task)
                .FirstOrDefaultAsync(x => x.Id == resourceId, ct);

            if (entity is null) return false;

            var calcId = entity.Task.CalculationId;

            // optimistic concurrency: detect stale edits
            if (res.RowVersion is { Length: > 0 })
                context.Entry(entity).Property(x => x.RowVersion).OriginalValue = res.RowVersion;

            entity.Update(res);
            entity.Metadata.QuantityParam = res.Data.QuantityParam;

            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyConflictException("Resource", resourceId);
            }

            var fullResource = await context.Resources
                .Where(x => x.Id == entity.Id)
                .Include(x => x.Offers)
                .IncludeResourceLookups()
                .FirstOrDefaultAsync(ct);

            if (fullResource is null) return false;

            await notification.SendNotificationAsync(
                calcId.ToString(),
                ObjectTypHub.resource,
                OperationType.Update,
                fullResource.MapToResourceListDTO());

            return true;
        }
    }
}
