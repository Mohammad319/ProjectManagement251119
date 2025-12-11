using Application.Extention;
using Application.Feature.Calculation.Resource;
using Application.Interfaces;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;

namespace Persistence.Service
{
    public class ResourceService(ShardingSingleDbContext dataAccess, INotificationHub notification) : IResourceService
    {

        // -----------------------------------------------------
        // Copy
        // -----------------------------------------------------
        public async Task<bool> CopyAsync(IReadOnlyList<ResourceTaskItemDTO> Items, int parentTaskId, int sourceCalcId, CancellationToken cancellationToken = default)
        {
            var parent = await dataAccess.Tasks
                .Where(x => x.Id == parentTaskId)
                .Select(x => new
                {
                    MaxOrder = x.Resources.Max(r => (double?)r.SortOrder),
                    NewCalcID = x.CalculationId,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (parent is null)
                return false;

            List<ResourceEntity> entities = [];
            double nextOrder = parent.MaxOrder.HasValue ? parent.MaxOrder.Value + 100 : 0;

            foreach (var item in Items)
            {
                var res = await dataAccess.Resources
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == item.Id, cancellationToken);

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

            dataAccess.Resources.AddRange(entities);
            await dataAccess.SaveChangesAsync(cancellationToken);

            var newIds = entities.Select(x => x.Id).ToList();

            var loadedEntities = await dataAccess.Resources
                .Where(r => newIds.Contains(r.Id))
                .Include(r => r.Account)
                .Include(r => r.Status)
                .Include(r => r.Opportunity)
                .Include(r => r.ResourceSort)
                .Include(r => r.ResourceType)
                .ToListAsync(cancellationToken);

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
        public async Task<bool> CreateAsync(IReadOnlyList<ResourcePostDTO> Items, int parentTaskId, CancellationToken cancellationToken = default)
        {
            var parent = await dataAccess.Tasks
                .Where(x => x.Id == parentTaskId)
                .Select(x => new
                {
                    MaxOrder = x.Resources.Max(r => (double?)r.SortOrder),
                    CalID = x.CalculationId,
                })
                .FirstOrDefaultAsync(cancellationToken);

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

            dataAccess.Resources.AddRange(entities);
            await dataAccess.SaveChangesAsync(cancellationToken);

            var newIds = entities.Select(x => x.Id).ToList();

            var loadedEntities = await dataAccess.Resources
                .Where(r => newIds.Contains(r.Id))
                .Include(r => r.Account)
                .Include(r => r.Status)
                .Include(r => r.Opportunity)
                .Include(r => r.ResourceSort)
                .Include(r => r.ResourceType)
                .ToListAsync(cancellationToken);

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
        public async Task<bool> CutAsync(int TaskId, int sourceCalcId, IReadOnlyList<ResourceTaskItemDTO> items, CancellationToken cancellationToken = default)
        {
            var parent = await dataAccess.Tasks
                .Where(x => x.Id == TaskId)
                .Select(x => new
                {
                    CalID = x.CalculationId,
                    Max = x.Resources.Max(x => (double?)x.SortOrder),
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (parent == null)
                return false;

            List<ResourceEntity> entities = [];

            foreach (var item in items)
            {
                var resource = await dataAccess.Resources.FindAsync(item.Id);

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

            dataAccess.Resources.UpdateRange(entities);
            await dataAccess.SaveChangesAsync(cancellationToken);

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
                    dataAccess.Resources.Entry(item).Reference(p => p.Account).Load();
                    dataAccess.Resources.Entry(item).Reference(p => p.Status).Load();
                    dataAccess.Resources.Entry(item).Reference(p => p.Opportunity).Load();
                    dataAccess.Resources.Entry(item).Reference(p => p.ResourceSort).Load();
                    dataAccess.Resources.Entry(item).Reference(p => p.ResourceType).Load();
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
        public async Task<bool> DeleteAsync(IEnumerable<int> resourceIds, int calcId, CancellationToken cancellationToken = default)
        {
            List<int> deletedItems = [];

            foreach (int id in resourceIds)
            {
                var res = await dataAccess.Resources
                    .FirstOrDefaultAsync(x => x.Id == id && x.Task.CalculationId == calcId, cancellationToken);

                if (res == null)
                    continue;

                dataAccess.Resources.Remove(res);
                deletedItems.Add(id);
            }

            await dataAccess.SaveChangesAsync(cancellationToken);

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
        public async Task<bool> NewOrderAsync(int Id, double NewOrder, CancellationToken cancellationToken = default)
        {
            var resource = await dataAccess.Resources
                .Where(x => x.Id == Id)
                .Select(x => new
                {
                    Res = x,
                    CalID = x.Task.CalculationId,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (resource == null)
                return false;

            resource.Res.SortOrder = NewOrder;
            dataAccess.Resources.Update(resource.Res);
            await dataAccess.SaveChangesAsync(cancellationToken);

            var fullResource = await dataAccess.Resources
                .Where(x => x.Id == Id)
                .Include(x => x.Offers)
                .Include(x => x.Account)
                .Include(x => x.Status)
                .Include(x => x.ResourceType)
                .Include(x => x.ResourceSort)
                .Include(x => x.Opportunity)
                .FirstOrDefaultAsync(cancellationToken);

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
        public async Task<bool> UpdateAsync(int resourceId, ResourcePostDTO res, CancellationToken cancellationToken = default)
        {
            // نجيب الكيان مع الـ Task للحصول على CalcId
            var entity = await dataAccess.Resources
                .Include(x => x.Task)
                .FirstOrDefaultAsync(x => x.Id == resourceId, cancellationToken);

            if (entity == null)
                return false;

            var calcId = entity.Task.CalculationId;

            // نحدّث الكيان في مكانه باستخدام الدالة الموجودة في ResourceEntity
            entity.Update(res);

            // نحافظ على QuantityParam لو عندك منطق خاص لها
            entity.Metadata.QuantityParam = res.Data.QuantityParam;

            dataAccess.Resources.Update(entity);
            await dataAccess.SaveChangesAsync(cancellationToken);

            var fullResource = await dataAccess.Resources
                .Where(x => x.Id == entity.Id)
                .Include(x => x.ResourceType)
                .Include(x => x.ResourceSort)
                .Include(x => x.Offers)
                .Include(x => x.Account)
                .Include(x => x.Status)
                .Include(x => x.Opportunity)
                .FirstOrDefaultAsync(cancellationToken);

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
