using Application.Extention;
using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Services.CalculationItems.Resource
{
    public interface IResourceService
    {
        Task<bool> CopyAsync(IReadOnlyList<ResourceTaskItemDTO> items, int parentTaskId,
            int sourceCalcId, CancellationToken cancellationToken = default);
        Task<bool> CreateAsync(IReadOnlyList<ResourcePostDTO> Items, int targetCalcId, CancellationToken cancellationToken = default);
        Task<bool> CutAsync(int TaskId, int sourceCalcId, IReadOnlyList<ResourceTaskItemDTO> items, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(IEnumerable<int> resourceIds, int calcId, CancellationToken cancellationToken = default);
        Task<bool> NewOrderAsync(int Id, double NewOrder, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(int taskId, ResourcePostDTO dto, CancellationToken cancellationToken = default);
    }
    public class ResourceService(IShardingSingleDbContext _dataAccess, INotificationHub notification) : IResourceService
    {
        public async Task<bool> CopyAsync(IReadOnlyList<ResourceTaskItemDTO> Items, int parentTaskId, int sourceCalcId, CancellationToken cancellationToken = default)
        {
            var Parent = await _dataAccess.Tasks.Where(x => x.Id == parentTaskId).Select(x => new
            {
                MaxOrder = x.Resources.Max(r => (double?)r.SortOrder),
                NewCalcID = x.CalculationId,
            }).FirstOrDefaultAsync(cancellationToken);
            if (Parent is null) return false;
            List<ResourceEntity> Entities = [];
            double nextOrder = Parent.MaxOrder.HasValue ? Parent.MaxOrder.Value + 100 : 0;

            foreach (var item in Items)
            {
                var res = await _dataAccess.Resource
                    .AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.Id, cancellationToken);
                if (res == null) continue;
                res.Id = 0;
                res.OfferId = null;
                if (sourceCalcId != Parent.NewCalcID)
                {
                    if (!string.IsNullOrEmpty(res.Metadata.QuantityParam))
                        res.Metadata.QuantityParam = PMValuesConst.FixedQ;
                    res.Metadata.Quantity = item.Value;
                    res.OpportunityId = null;
                }
                res.TaskId = parentTaskId;
                res.SortOrder = nextOrder;
                nextOrder += 100;
                Entities.Add(res);
            }
            _dataAccess.Resource.AddRange(Entities);
            await _dataAccess.SaveChangesAsync(cancellationToken);
            var newIds = Entities.Select(x => x.Id).ToList();
            var loadedEntities = await _dataAccess.Resource.Where(r => newIds.Contains(r.Id))
                .Include(r => r.Account).Include(r => r.Status).Include(r => r.Opportunity)
                .Include(r => r.ResourceSort).Include(r => r.ResourceType).ToListAsync(cancellationToken);

            var resourceDtos = loadedEntities.Select(ResourceExtention.MapToResourceListDTO).ToList();
            await notification.SendNotificationAsync(Parent.NewCalcID.ToString(), ObjectTypHub.resource, OperationType.AddRange, parentTaskId, resourceDtos);
            return true;
        }

        public async Task<bool> CreateAsync(IReadOnlyList<ResourcePostDTO> Items, int parentTaskId, CancellationToken cancellationToken = default)
        {
            var parent = await _dataAccess.Tasks.Where(x => x.Id == parentTaskId).Select(x => new
            {
                MaxOrder = x.Resources.Max(r => (double?)r.SortOrder),
                CalID = x.CalculationId,
            }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
            if (parent == null) return false;
            List<ResourceEntity> Entities = [];
            double nextOrder = parent.MaxOrder.HasValue ? parent.MaxOrder.Value + 100 : 0;

            foreach (var resPost in Items)
            {
                ResourceEntity resource = resPost.Parse(parentTaskId);
                resource.SortOrder = nextOrder;
                nextOrder += 100;
                Entities.Add(resource);
            }
            _dataAccess.Resource.AddRange(Entities);
            await _dataAccess.SaveChangesAsync(cancellationToken);
            var newIds = Entities.Select(x => x.Id).ToList();
            var loadedEntities = await _dataAccess.Resource.Where(r => newIds.Contains(r.Id))
                .Include(r => r.Account).Include(r => r.Status).Include(r => r.Opportunity)
                .Include(r => r.ResourceSort).Include(r => r.ResourceType).ToListAsync(cancellationToken);
            var resourceDtos = loadedEntities.Select(ResourceExtention.MapToResourceListDTO).ToList();

            await notification.SendNotificationAsync(parent.CalID.ToString(),
                    ObjectTypHub.resource, OperationType.AddRange, parentTaskId, resourceDtos);
            return true;
        }

        public async Task<bool> CutAsync(int TaskId, int sourceCalcId, IReadOnlyList<ResourceTaskItemDTO> items, CancellationToken cancellationToken = default)
        {
            var Parent = await _dataAccess.Tasks.Where(x => x.Id == TaskId).Select(x => new
            {
                CalID = x.CalculationId,
                Max = x.Resources.Max(x => (int?)x.SortOrder),
            }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
            if (Parent == null) return false;
            List<ResourceEntity> Entities = [];

            foreach (var id in items)
            {
                var Resource = await _dataAccess.Resource.FindAsync(id.Id);
                if (Resource == null || Resource.TaskId == TaskId) continue;
                if (Parent.CalID != sourceCalcId)
                {
                    if (!string.IsNullOrEmpty(Resource.Metadata.QuantityParam))
                        Resource.Metadata.QuantityParam = PMValuesConst.FixedQ;
                    Resource.Offers = null;
                    Resource.OfferId = null;
                    Resource.OpportunityId = null;
                }
                Resource.SortOrder = Parent.Max.HasValue ? Parent.Max.Value + 100 : 0;
                Resource.TaskId = TaskId;

                Entities.Add(Resource);
            }
            _dataAccess.Resource.UpdateRange(Entities);
            await _dataAccess.SaveChangesAsync(cancellationToken);

            if (sourceCalcId == Parent.CalID)
                await notification.SendNotificationAsync(sourceCalcId.ToString(), ObjectTypHub.resource, OperationType.MoveRange, Tuple.Create(TaskId, Entities.Select(x => x.Id)));
            else
            {
                foreach (var item in Entities)
                {
                    _dataAccess.Resource.Entry(item).Reference(p => p.Account).Load();
                    _dataAccess.Resource.Entry(item).Reference(p => p.Status).Load();
                    _dataAccess.Resource.Entry(item).Reference(p => p.Opportunity).Load();
                    _dataAccess.Resource.Entry(item).Reference(p => p.ResourceSort).Load();
                    _dataAccess.Resource.Entry(item).Reference(p => p.ResourceType).Load();
                }
                List<ResourceListDTO> ListHub = [];// entities.Select(TaskExpression.SelectTaskListDTO.Compile()).ToList();
                foreach (var item in Entities) ListHub.Add(item.MapToResourceListDTO());

                await notification.SendNotificationAsync(sourceCalcId.ToString(), ObjectTypHub.resource, OperationType.RemoveRange, Entities.Select(x => x.Id));
                await notification.SendNotificationAsync(Parent.CalID.ToString(), ObjectTypHub.resource, OperationType.AddRange, TaskId, ListHub);
            }
            return true;
        }

        public async Task<bool> DeleteAsync(IEnumerable<int> resourceIds, int calcId, CancellationToken cancellationToken = default)
        {
            List<int> DeletedItems = [];
            foreach (int id in resourceIds)
            {
                var res = await _dataAccess.Resource.FirstOrDefaultAsync(x => x.Id == id &&
                x.Task.CalculationId == calcId, cancellationToken: cancellationToken);
                if (res == null) continue;
                _dataAccess.Resource.Remove(res);
                DeletedItems.Add(id);
            }
            await _dataAccess.SaveChangesAsync(cancellationToken);
            await notification.SendNotificationAsync(calcId.ToString(), ObjectTypHub.resource, OperationType.RemoveRange, DeletedItems);
            return true;
        }

        public async Task<bool> NewOrderAsync(int Id, double NewOrder, CancellationToken cancellationToken = default)
        {
            var Resource = await _dataAccess.Resource.Where(x => x.Id == Id).Select(x => new
            {
                Res = x,
                CalID = x.Task.CalculationId,
            }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
            if (Resource == null)
                return false;

            Resource.Res.SortOrder = NewOrder;
            _dataAccess.Resource.Update(Resource.Res);
            await _dataAccess.SaveChangesAsync(cancellationToken);

            var fullResource = await _dataAccess.Resource
                .Where(x => x.Id == Id)
                .Include(x => x.Offers)
                .Include(x => x.Account)
                .Include(x => x.Status)
                .Include(x => x.ResourceType)
                .Include(x => x.ResourceSort)
                .Include(x => x.Opportunity)
                .FirstOrDefaultAsync(cancellationToken);

            var taskList = fullResource.MapToResourceListDTO();
            await notification.SendNotificationAsync(Resource.CalID.ToString(), ObjectTypHub.resource, OperationType.Update, taskList);

            return true;
        }

        public async Task<bool> UpdateAsync(int taskId, ResourcePostDTO res, CancellationToken cancellationToken = default)
        {
            var resourceData = await _dataAccess.Resource
                .AsNoTracking()
                .Where(x => x.Id == taskId)
                .Select(x => new
                {
                    Original = x,
                    CalcId = x.Task.CalculationId
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (resourceData == null)
                return false;

            // نسخ الخصائص من DTO إلى الكيان الجديد
            var resourcePost = new ResourcePostDTO();
            res.CopyPropertiesTo(resourcePost);

            var updatedEntity = resourcePost.Parse(resourceData.Original.TaskId);
            updatedEntity.Id = resourceData.Original.Id;
            updatedEntity.SortOrder = resourceData.Original.SortOrder;
            updatedEntity.Metadata.QuantityParam = res.Data.QuantityParam;

            // تحديث الكيان
            _dataAccess.Resource.Update(updatedEntity);
            await _dataAccess.SaveChangesAsync(cancellationToken);

            var fullResource = await _dataAccess.Resource
                .Where(x => x.Id == updatedEntity.Id)
                .Include(x => x.ResourceType)
                .Include(x => x.ResourceSort)
                .Include(x => x.Offers)
                .Include(x => x.Account)
                .Include(x => x.Status)
                .Include(x => x.Opportunity)
                .FirstOrDefaultAsync(cancellationToken);

            var dto = fullResource.MapToResourceListDTO();

            // إرسال إشعار عبر Hub
            await notification.SendNotificationAsync(
                resourceData.CalcId.ToString(),
                ObjectTypHub.resource,
                OperationType.Update,
                dto
            );

            return true;
        }
    }
}
