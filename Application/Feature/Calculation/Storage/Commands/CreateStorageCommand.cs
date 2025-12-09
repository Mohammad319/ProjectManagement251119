using Application.Extention;
using Application.Interfaces;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Feature.Calculation.Storage.Commands
{
    public sealed record CreateStorageCommand(CalculationItemType Type, AuthorityStorage Level, StorageSort Sort, int Id, int UserId, int? DepartmentId) : IRequest<bool>;

    public class CreateStorageCommandHandler(IShardingSingleDbContext _dataAccess) : IRequestHandler<CreateStorageCommand, bool>
    {
        public async Task<bool> Handle(CreateStorageCommand request, CancellationToken cancellationToken)
        {
            object obj = null;
            if (request.Type == CalculationItemType.task)
            {
                var tasks = await _dataAccess.Tasks.FromSqlRaw("EXEC GetRecursiveTasks {0}", request.Id).IgnoreQueryFilters().AsNoTracking().ToListAsync(cancellationToken);
                if (tasks == null) return false;

                var taskIds = tasks.Select(t => t.Id).ToList();
                var resources = await _dataAccess.Resource.Where(r => taskIds.Contains(r.TaskId))
                    .AsNoTracking().ToListAsync(cancellationToken);
                foreach (var ts in tasks)
                    ts.Resources = [.. resources.Where(r => r.TaskId == ts.Id)];
                TaskExtention.BuildTaskHierarchy(tasks);
                var task = tasks.FirstOrDefault(x => x.Id == request.Id);
                task = TaskExtention.Reset(task);
                obj = task;
            }
            else if (request.Type == CalculationItemType.resource)
            {
                obj = await _dataAccess.Resource.AsNoTracking().Where(x => x.Id == request.Id).Select(x => new
                {
                    x.ResourceTypeId,
                    x.Name,
                    x.AccountId,
                    x.Active,
                    x.ResType,
                    x.StatusId,
                    x.ResourceSortId,
                    x.Metadata,
                }).FirstOrDefaultAsync(cancellationToken);
            }

            if (obj != null)
            {
                var st = new StorageEntity()
                {
                    StorageSort = request.Sort,
                    StorageLevel = request.Level,
                    StorageType = request.Type,
                    DepartmentId = request.DepartmentId ?? 0,
                    CreatedBy = request.UserId,
                    StorageValue = JsonSerializer.Serialize(obj)
                };
                _dataAccess.Storage.Add(st);
                await _dataAccess.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }
    }
}
