using Application.Extention;
using Application.Interfaces;
using Application.Mapping.CalcItems;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Services.CalculationItems.Task
{
    public interface ITaskService
    {
        Task<bool> CopyAsync(IReadOnlyList<ResourceTaskItemDTO> taskItems, int? parentTaskId, int sourceCalcId, int targetCalcId, bool isOH, bool deleteOriginal = false, CancellationToken cancellationToken = default);
        Task<bool> CreateAsync(IReadOnlyList<TaskPostDTO> tasks, int targetCalcId, CancellationToken cancellationToken = default);
        Task<bool> CutAsync(int sourceCalcId, int targetCalcId, int? parentTaskId, IReadOnlyList<ResourceTaskItemDTO> items, double order = 100, bool isOH = false, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(IEnumerable<int> taskIds, int calcId, CancellationToken cancellationToken = default);
        Task<bool> NewOrderAsync(int taskId, double newOrder, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(int taskId, TaskPostDTO dto, CancellationToken cancellationToken = default);
    }

    public class TaskService : ITaskService
    {
        private readonly IShardingSingleDbContext _context;
        private readonly INotificationHub _notification;

        public TaskService(IShardingSingleDbContext context, INotificationHub notification)
        {
            _context = context;
            _notification = notification;
        }

        public async Task<bool> CopyAsync(IReadOnlyList<ResourceTaskItemDTO> taskItems, int? parentTaskId, int sourceCalcId, int targetCalcId, bool isOH, bool deleteOriginal = false, CancellationToken cancellationToken = default)
        {
            var resultDtos = new List<TaskListDTO>();
            double order = await GetMaxOrderAsync(sourceCalcId, parentTaskId, cancellationToken);

            foreach (var item in taskItems)
            {
                var clonedTasks = await CloneTasksAsync(item.Id, targetCalcId, sourceCalcId, isOH, deleteOriginal, parentTaskId, order, cancellationToken);
                if (clonedTasks != null && clonedTasks.Any())
                    resultDtos.AddRange(clonedTasks.MapToTaskListDTOs());

                order += 100;
            }

            await _notification.SendNotificationAsync(targetCalcId.ToString(), ObjectTypHub.task, OperationType.AddRange, resultDtos);
            return true;
        }

        public async Task<bool> CreateAsync(IReadOnlyList<TaskPostDTO> tasks, int targetCalcId, CancellationToken cancellationToken = default)
        {
            if (tasks == null || tasks.Count == 0)
                return false;

            int? parentTaskId = tasks.FirstOrDefault()?.TaskId;

            if (parentTaskId.HasValue && parentTaskId > 0)
            {
                var parentTask = await _context.Tasks.FirstOrDefaultAsync(x => x.Id == parentTaskId && x.CalculationId == targetCalcId, cancellationToken);
                if (parentTask == null) return false;
                if (tasks.Any(t => t.TaskId != parentTask.Id)) return false;

                foreach (var task in tasks)
                    task.Data.IsOH = parentTask.Metadata.IsOH;
            }
            else
            {
                bool calcExists = await _context.Calculation.AnyAsync(x => x.Id == targetCalcId, cancellationToken);
                if (!calcExists) return false;
            }

            var entities = tasks.Select(t => TaskMapper.MapToTaskEntity(t, targetCalcId)).ToList();

            _context.Tasks.AddRange(entities);
            await _context.SaveChangesAsync(cancellationToken);

            var entityIds = entities.Select(e => e.Id).ToList();
            await LoadTaskNavigationAsync(entityIds, cancellationToken);

            entities = TaskExtention.FlattenTasks(entities);
            await NotifyTasks(OperationType.AddRange, targetCalcId, entities, cancellationToken);

            return true;
        }

        public Task<bool> CutAsync(int sourceCalcId, int targetCalcId, int? parentTaskId, IReadOnlyList<ResourceTaskItemDTO> items, double order = 100, bool isOH = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteAsync(IEnumerable<int> taskIds, int calcId, CancellationToken cancellationToken = default)
        {
            if (taskIds == null) return false;

            var deletedTaskIds = new List<int>();

            foreach (var id in taskIds)
            {
                var tasksToDelete = await GetTaskWithChildrenAsync(id, cancellationToken);
                if (tasksToDelete != null && tasksToDelete.Any())
                {
                    deletedTaskIds.Add(id);
                    _context.Tasks.RemoveRange(tasksToDelete);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _notification.SendNotificationAsync(calcId.ToString(), ObjectTypHub.task, OperationType.RemoveRange, deletedTaskIds);

            return true;
        }

        public async Task<bool> NewOrderAsync(int taskId, double newOrder, CancellationToken cancellationToken = default)
        {
            var taskWithCalcId = await _context.Tasks.Where(x => x.Id == taskId).Select(x => new { Task = x, CalcId = x.CalculationId }).FirstOrDefaultAsync(cancellationToken);
            if (taskWithCalcId == null) return false;

            taskWithCalcId.Task.SortOrder = newOrder;
            return await UpdateTaskAsync(taskWithCalcId.CalcId, taskWithCalcId.Task, cancellationToken);
        }

        public async Task<bool> UpdateAsync(int taskId, TaskPostDTO dto, CancellationToken cancellationToken = default)
        {
            var taskWithCalcId = await _context.Tasks.Where(x => x.Id == taskId).Select(x => new { Task = x, CalcId = x.CalculationId }).FirstOrDefaultAsync(cancellationToken);
            if (taskWithCalcId == null) return false;

            var task = taskWithCalcId.Task;

            task.Name = dto.Name;
            task.StatusId = dto.StatusId;
            task.OpportunityId = dto.OpportunityId;
            task.Metadata = dto.Data;
            task.Metadata.QuantityParam = dto.Data.QuantityParam;

            if (dto.Data.Type != task.Metadata.Type && dto.Data.Type == TaskType.CodeName)
            {
                bool hasResources = await _context.Resource.AnyAsync(x => x.TaskId == task.Id, cancellationToken);
                if (hasResources)
                    return false;

                task.StatusId = null;
                task.OpportunityId = null;
                task.Metadata = new()
                {
                    IsOH = dto.Data.IsOH,
                    Type = TaskType.CodeName,
                    Quantity = null,
                };
            }

            return await UpdateTaskAsync(taskWithCalcId.CalcId, task, cancellationToken);
        }

        private async Task<bool> UpdateTaskAsync(int calcId, TaskEntity task, CancellationToken cancellationToken)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync(cancellationToken);
            await LoadTaskNavigationAsync(new List<int> { task.Id }, cancellationToken);
            var taskDto = task.MapToTaskListDTO();
            await _notification.SendNotificationAsync(calcId.ToString(), ObjectTypHub.task, OperationType.Update, taskDto);

            return true;
        }

        private async Task<List<TaskEntity>> GetTaskWithChildrenAsync(int rootTaskId, CancellationToken cancellationToken)
        {
            return await _context.Tasks
                .FromSqlRaw("EXEC GetRecursiveTasks {0}", rootTaskId)
                .IgnoreQueryFilters()
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        async Task<double> GetMaxOrderAsync(int deleteOriginal, int? TaskParentID, CancellationToken ct)
        {
            return TaskParentID.HasValue
                ? await _context.Tasks.Where(x => x.ParentTaskId == TaskParentID).MaxAsync(x => x.SortOrder, ct)
                : await _context.Tasks.Where(x => x.CalculationId == deleteOriginal && x.ParentTaskId == null).MaxAsync(x => x.SortOrder, ct);
        }
        public async Task<List<TaskEntity>> CloneTasksAsync(int rootTaskId, int targetCalcId, int sourceCalcId, bool isOH, bool deleteOriginal, int? parentTaskId, double order, CancellationToken cancellationToken)
        {
            var tasks = await GetTaskWithChildrenAsync(rootTaskId, cancellationToken);
            if (tasks == null || tasks.Count == 0) return null;

            var taskIds = tasks.Select(t => t.Id).ToList();

            var resources = await _context.Resource.Where(r => taskIds.Contains(r.TaskId))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            foreach (var task in tasks)
                task.Resources = [.. resources.Where(r => r.TaskId == task.Id)];

            var rootTask = tasks.FirstOrDefault(t => t.Id == rootTaskId);
            TaskExtention.BuildTaskHierarchy(tasks);
            TaskExtention.SetCalculationIdRecursive(tasks, isOH, targetCalcId, sourceCalcId);

            if (parentTaskId.HasValue)
                rootTask.ParentTaskId = parentTaskId;

            await _context.Tasks.AddAsync(rootTask, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await LoadTaskReferencesAsync(tasks, cancellationToken);

            return tasks;
        }

        private async System.Threading.Tasks.Task LoadTaskNavigationAsync(List<int> taskIds, CancellationToken cancellationToken)
        {
            if (taskIds == null || taskIds.Count == 0) return;

            await _context.Tasks.Where(t => taskIds.Contains(t.Id))
                .Include(t => t.Status)
                .Include(t => t.Opportunity)
                .LoadAsync(cancellationToken);
        }

        public async System.Threading.Tasks.Task LoadTaskReferencesAsync(List<TaskEntity> tasks, CancellationToken cancellationToken)
        {
            if (tasks == null || tasks.Count == 0) return;

            List<int> taskIds = [.. tasks.Select(t => t.Id)];
            await LoadTaskNavigationAsync(taskIds, cancellationToken);

            List<ResourceEntity> allResources = await _context.Resource.Where(r => taskIds.Contains(r.TaskId))
                .Include(r => r.Status)
                .Include(r => r.Account)
                .Include(r => r.ResourceType)
                .Include(r => r.ResourceSort)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            Dictionary<int, List<ResourceEntity>> resourceLookup = allResources.GroupBy(r => r.TaskId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var task in tasks)
                task.Resources = resourceLookup.TryGetValue(task.Id, out var resList) ? resList : new List<ResourceEntity>();
        }

        private async System.Threading.Tasks.Task NotifyTasks(OperationType operationType, int calcId, List<TaskEntity> tasks, CancellationToken cancellationToken)
        {
            var dtos = tasks.MapToTaskListDTOs();
            await _notification.SendNotificationAsync(calcId.ToString(), ObjectTypHub.task, operationType, dtos);
        }
    }
}
