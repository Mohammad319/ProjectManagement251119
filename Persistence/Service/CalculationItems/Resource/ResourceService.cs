using Application.Extention;
using Application.Feature.Calculation.Resource;
using Application.Interfaces;
using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace Persistence.Service
{
    public class ResourceService(IDbContextFactoryTenant dbFactory, INotificationHub notification) : IResourceService
    {

        // -----------------------------------------------------
        // Copy
        // -----------------------------------------------------
        public async Task<bool> CopyAsync(IReadOnlyList<ResourceTaskItemDTO> Items, int parentTaskId, int sourceCalcId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var parent = await context.Tasks
                .Where(x => x.Id == parentTaskId)
                .Select(x => new
                {
                    MaxOrder = x.Resources.Max(r => (double?)r.SortOrder),
                    NewCalcID = x.CalculationId,
                })
                .FirstOrDefaultAsync(ct);

            if (parent is null)
                return false;

            List<ResourceEntity> entities = [];
            double nextOrder = parent.MaxOrder.HasValue ? parent.MaxOrder.Value + 100 : 0;

            foreach (var item in Items)
            {
                var res = await context.Resources
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == item.Id, ct);

                if (res == null)
                    continue;

                // نحوله لنسخة جديدة
                res.Id = 0;
                res.PrimaryOfferId = null;

                if (sourceCalcId != parent.NewCalcID)
                {
                    if (!string.IsNullOrEmpty(res.Metadata.QuantityParam))
                        res.Metadata.QuantityParam = PMValuesConst.FixedQ;

                    res.Metadata.Quantity = item.Value;
                    res.OpportunityId = null;
                }

                res.TaskId = parentTaskId;
                res.SortOrder = nextOrder;
                nextOrder += 100;

                entities.Add(res);
            }

            context.Resources.AddRange(entities);
            await context.SaveChangesAsync(ct);

            var newIds = entities.Select(x => x.Id).ToList();

            var loadedEntities = await context.Resources
                .Where(r => newIds.Contains(r.Id))
                .Include(r => r.Account)
                .Include(r => r.Status)
                .Include(r => r.Opportunity)
                .Include(r => r.ResourceSort)
                .Include(r => r.ResourceType)
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
        public async Task<bool> CreateAsync(IReadOnlyList<ResourcePostDTO> Items, int parentTaskId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var parent = await context.Tasks
                .Where(x => x.Id == parentTaskId)
                .Select(x => new
                {
                    MaxOrder = x.Resources.Max(r => (double?)r.SortOrder),
                    CalID = x.CalculationId,
                })
                .FirstOrDefaultAsync(ct);

            if (parent == null)
                return false;

            List<ResourceEntity> entities = [];
            double nextOrder = parent.MaxOrder.HasValue ? parent.MaxOrder.Value + 100 : 0;

            foreach (var dto in Items)
            {
                // نفترض لديك إما:
                //  - امتداد Parse(dto, parentTaskId) محدث يتعامل مع CostValue
                //  أو تستخدم دالة Create في الكيان
                // هنا أستخدم نمط Create على الكيان (أنصح به):

                var resource = ResourceEntity.Create(parentTaskId, dto, nextOrder);
                nextOrder += 100;

                entities.Add(resource);
            }

            context.Resources.AddRange(entities);
            await context.SaveChangesAsync(ct);

            var newIds = entities.Select(x => x.Id).ToList();

            var loadedEntities = await context.Resources
                .Where(r => newIds.Contains(r.Id))
                .Include(r => r.Account)
                .Include(r => r.Status)
                .Include(r => r.Opportunity)
                .Include(r => r.ResourceSort)
                .Include(r => r.ResourceType)
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
        public async Task<bool> CutAsync(int TaskId, int sourceCalcId, IReadOnlyList<ResourceTaskItemDTO> items, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var parent = await context.Tasks
                .Where(x => x.Id == TaskId)
                .Select(x => new
                {
                    CalID = x.CalculationId,
                    Max = x.Resources.Max(x => (double?)x.SortOrder),
                })
                .FirstOrDefaultAsync(ct);

            if (parent == null)
                return false;

            List<ResourceEntity> entities = [];

            foreach (var item in items)
            {
                var resource = await context.Resources.FindAsync(item.Id);

                if (resource == null || resource.TaskId == TaskId)
                    continue;

                if (parent.CalID != sourceCalcId)
                {
                    if (!string.IsNullOrEmpty(resource.Metadata.QuantityParam))
                        resource.Metadata.QuantityParam = PMValuesConst.FixedQ;

                    resource.Offers = null;
                    resource.PrimaryOfferId = null;
                    resource.OpportunityId = null;
                }

                resource.SortOrder = parent.Max.HasValue ? parent.Max.Value + 100 : 0;
                resource.TaskId = TaskId;

                entities.Add(resource);
            }

            context.Resources.UpdateRange(entities);
            await context.SaveChangesAsync(ct);

            if (sourceCalcId == parent.CalID)
            {
                await notification.SendNotificationAsync(
                    sourceCalcId.ToString(),
                    ObjectTypHub.resource,
                    OperationType.MoveRange,
                    Tuple.Create(TaskId, entities.Select(x => x.Id)));
            }
            else
            {
                foreach (var item in entities)
                {
                    context.Resources.Entry(item).Reference(p => p.Account).Load();
                    context.Resources.Entry(item).Reference(p => p.Status).Load();
                    context.Resources.Entry(item).Reference(p => p.Opportunity).Load();
                    context.Resources.Entry(item).Reference(p => p.ResourceSort).Load();
                    context.Resources.Entry(item).Reference(p => p.ResourceType).Load();
                }

                List<ResourceListDTO> listHub = [];
                foreach (var item in entities)
                    listHub.Add(item.MapToResourceListDTO());

                await notification.SendNotificationAsync(
                    sourceCalcId.ToString(),
                    ObjectTypHub.resource,
                    OperationType.RemoveRange,
                    entities.Select(x => x.Id));

                await notification.SendNotificationAsync(
                    parent.CalID.ToString(),
                    ObjectTypHub.resource,
                    OperationType.AddRange,
                    TaskId,
                    listHub);
            }

            return true;
        }

        // -----------------------------------------------------
        // Delete
        // -----------------------------------------------------
        public async Task<bool> DeleteAsync(IEnumerable<int> resourceIds, int calcId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            List<int> deletedItems = [];

            foreach (int id in resourceIds)
            {
                var res = await context.Resources
                    .FirstOrDefaultAsync(x => x.Id == id && x.Task.CalculationId == calcId, ct);

                if (res == null)
                    continue;

                context.Resources.Remove(res);
                deletedItems.Add(id);
            }

            await context.SaveChangesAsync(ct);

            await notification.SendNotificationAsync(
                calcId.ToString(),
                ObjectTypHub.resource,
                OperationType.RemoveRange,
                deletedItems);

            return true;
        }

        // -----------------------------------------------------
        // New Order
        // -----------------------------------------------------
        public async Task<bool> NewOrderAsync(int Id, double NewOrder, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var resource = await context.Resources
                .Where(x => x.Id == Id)
                .Select(x => new
                {
                    Res = x,
                    CalID = x.Task.CalculationId,
                })
                .FirstOrDefaultAsync(ct);

            if (resource == null)
                return false;

            resource.Res.SortOrder = NewOrder;
            context.Resources.Update(resource.Res);
            await context.SaveChangesAsync(ct);

            var fullResource = await context.Resources
                .Where(x => x.Id == Id)
                .Include(x => x.Offers)
                .Include(x => x.Account)
                .Include(x => x.Status)
                .Include(x => x.ResourceType)
                .Include(x => x.ResourceSort)
                .Include(x => x.Opportunity)
                .FirstOrDefaultAsync(ct);

            var dto = fullResource.MapToResourceListDTO();

            await notification.SendNotificationAsync(
                resource.CalID.ToString(),
                ObjectTypHub.resource,
                OperationType.Update,
                dto);

            return true;
        }

        // -----------------------------------------------------
        // Update
        // -----------------------------------------------------
        public async Task<bool> UpdateAsync(int resourceId, ResourcePostDTO res, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            // نجيب الكيان مع الـ Task للحصول على CalcId
            var entity = await context.Resources
                .Include(x => x.Task)
                .FirstOrDefaultAsync(x => x.Id == resourceId, ct);

            if (entity == null)
                return false;

            var calcId = entity.Task.CalculationId;

            // نحدّث الكيان في مكانه باستخدام الدالة الموجودة في ResourceEntity
            entity.Update(res);

            // نحافظ على QuantityParam لو عندك منطق خاص لها
            entity.Metadata.QuantityParam = res.Data.QuantityParam;

            context.Resources.Update(entity);
            await context.SaveChangesAsync(ct);

            var fullResource = await context.Resources
                .Where(x => x.Id == entity.Id)
                .Include(x => x.ResourceType)
                .Include(x => x.ResourceSort)
                .Include(x => x.Offers)
                .Include(x => x.Account)
                .Include(x => x.Status)
                .Include(x => x.Opportunity)
                .FirstOrDefaultAsync(ct);

            var dto = fullResource.MapToResourceListDTO();

            await notification.SendNotificationAsync(
                calcId.ToString(),
                ObjectTypHub.resource,
                OperationType.Update,
                dto);

            return true;
        }
    }
}
