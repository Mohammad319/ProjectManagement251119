
using Application.Extention;
using Application.Interfaces;
using Application.Mapping.CalcItems;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Feature.Calculation.Task.Commands
{
    public class CutTaskCommand : IRequest<bool>
    {
        public int OldCalcId { get; set; }
        public int NewNetCalcId { get; set; }
        public int? TaskParentID { get; set; }
        public List<ResourceTaskItemDTO> Items;
        public double Order { get; set; } = 100;
        public bool IsOH = false;

        public class CutTaskCommandHandler(IShardingSingleDbContext _dataAccess, INotificationHub notification) : IRequestHandler<CutTaskCommand, bool>
        {
            public async Task<bool> Handle(CutTaskCommand request, CancellationToken cancellationToken)
            {
                double? Max = 0;
                if (request.TaskParentID == 0) request.TaskParentID = null;
                if (request.TaskParentID.HasValue && request.TaskParentID > 0)
                {
                    var parent = await _dataAccess.Tasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.TaskParentID && x.CalculationId == request.NewNetCalcId, cancellationToken: cancellationToken);
                    if (parent == null) return false;
                    request.IsOH = parent.Metadata.IsOH;
                    if (parent.Tasks == null || parent.Tasks.Count == 0) Max = null;
                    else Max = parent?.Tasks?.Max(x => x.SortOrder);
                }
                else
                {
                    var Calc = await _dataAccess.Calculations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.NewNetCalcId, cancellationToken: cancellationToken);
                    if (Calc == null) return false;
                    if (Calc.Tasks == null || Calc.Tasks.Count == 0) Max = null;
                    else Max = Calc?.Tasks?.Max(x => x.SortOrder);
                }
                if (Max == null) Max = 0;
                else Max += 100;
                List<TaskEntity> Entities = [];

                foreach (var item in request.Items)
                {
                    if (request.OldCalcId == request.NewNetCalcId)
                    {
                        var task = await _dataAccess.Tasks.AsNoTracking().Where(x =>
    x.Id == item.Id && x.CalculationId == request.OldCalcId).FirstOrDefaultAsync(cancellationToken);
                        task.Metadata.IsOH = request.IsOH;
                        TaskExtention.SetNetCalcId(task);
                        task.ParentTaskId = request.TaskParentID;
                        _dataAccess.Tasks.Update(task);
                        Entities.Add(task);
                    }
                    else
                    {
                        var tasks = await _dataAccess.Tasks.FromSqlRaw("EXEC GetRecursiveTasks {0}", item.Id)
    .IgnoreQueryFilters().AsNoTracking().ToListAsync(cancellationToken);
                        if (tasks == null) continue;
                        var taskIds = tasks.Select(t => t.Id).ToList();
                        var resources = await _dataAccess.Resources.Where(r => taskIds.Contains(r.TaskId))
                            .AsNoTracking().ToListAsync(cancellationToken);
                        foreach (var t in tasks)
                        {
                            t.Resources = resources.Where(r => r.TaskId == t.Id).ToList();
                        }
                        var task = tasks.FirstOrDefault(x => x.ParentTaskId == null);
                        TaskExtention.BuildTaskHierarchy(tasks);
                        task = TaskExtention.Reset(task);

                        var task2 = await _dataAccess.Tasks.AsNoTracking().FirstOrDefaultAsync(x =>
    x.Id == item.Id && x.CalculationId == request.OldCalcId);
                        _dataAccess.Tasks.Remove(task2);
                        await _dataAccess.SaveChangesAsync(cancellationToken);
                        task.ParentTaskId = request.TaskParentID;
                        if (task.CalculationId != request.NewNetCalcId && !string.IsNullOrEmpty(task.Metadata.QuantityParam))
                            task.Metadata.QuantityParam = PMValuesConst.FixedQ;
                        task.Metadata.Quantity = item.Value;
                        task.CalculationId = request.NewNetCalcId;
                        task.Metadata.IsOH = request.IsOH;
                        task.SortOrder = Max.Value;
                        Max += 100;

                        TaskExtention.SetNetCalcId(task);

                        _dataAccess.Tasks.Add(task);
                        Entities.Add(task);
                    }
                }
                await _dataAccess.SaveChangesAsync(cancellationToken);
                foreach (var item in Entities)
                {
                    _dataAccess.Tasks.Entry(item).Reference(p => p.Status).Load();
                    _dataAccess.Tasks.Entry(item).Reference(p => p.Opportunity).Load();
                }
                List<TaskListDTO> ListHub = [];
                foreach (var item in Entities) ListHub.Add(item.MapToTaskListDTO());

                if (request.OldCalcId == request.NewNetCalcId)
                    await notification.SendNotificationAsync(request.NewNetCalcId.ToString(), ObjectTypHub.task, OperationType.MoveRange, Tuple.Create(ListHub, Entities.Select(x => x.Id)));
                else
                {
                    await notification.SendNotificationAsync(request.OldCalcId.ToString(), ObjectTypHub.task, OperationType.RemoveRange, Entities.Select(x => x.Id));
                    await notification.SendNotificationAsync(request.NewNetCalcId.ToString(), ObjectTypHub.task, OperationType.AddRange, ListHub);
                }

                return true;
            }
        }

    }

}
