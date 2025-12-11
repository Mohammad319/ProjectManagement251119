using Application.Extention;
using Application.Feature.Calculation.Task;
using Application.Interfaces;
using Application.Mapping.CalcItems;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;

namespace Persistence.Service.CalculationItems
{
    public class TaskService(ShardingSingleDbContext context, INotificationHub notification) : ITaskService
    {

        // -----------------------------------------------------
        // Copy tasks (with hierarchy + resources)
        // -----------------------------------------------------
        public async Task<bool> CopyAsync(
            IReadOnlyList<ResourceTaskItemDTO> taskItems,
            int? parentTaskId,
            int sourceCalcId,
            int targetCalcId,
            bool isOH,
            bool deleteOriginal = false,
            CancellationToken cancellationToken = default)
        {
            if (taskItems is null || taskItems.Count == 0)
                return false;

            var resultDtos = new List<TaskListDTO>();

            // ✅ نحسب الـ Order في الـ Target Calculation (وليس المصدر)
            double order = await GetMaxOrderAsync(targetCalcId, parentTaskId, cancellationToken);

            foreach (var item in taskItems)
            {
                var clonedTasks = await CloneTasksAsync(
                    rootTaskId: item.Id,
                    targetCalcId: targetCalcId,
                    sourceCalcId: sourceCalcId,
                    isOH: isOH,
                    deleteOriginal: deleteOriginal,
                    parentTaskId: parentTaskId,
                    order: order,
                    cancellationToken: cancellationToken);

                if (clonedTasks is not null && clonedTasks.Any())
                {
                    resultDtos.AddRange(clonedTasks.MapToTaskListDTOs());
                }

                order += 100;
            }

            await notification.SendNotificationAsync(
                targetCalcId.ToString(),
                ObjectTypHub.task,
                OperationType.AddRange,
                resultDtos);

            return true;
        }

        // -----------------------------------------------------
        // Create tasks
        // -----------------------------------------------------
        public async Task<bool> CreateAsync(
            IReadOnlyList<TaskPostDTO> tasks,
            int targetCalcId,
            CancellationToken cancellationToken = default)
        {
            if (tasks is null || tasks.Count == 0)
                return false;

            int? parentTaskId = tasks.FirstOrDefault()?.ParentTaskId;

            if (parentTaskId.HasValue && parentTaskId > 0)
            {
                var parentTask = await context.Tasks
                    .FirstOrDefaultAsync(x => x.Id == parentTaskId && x.CalculationId == targetCalcId, cancellationToken);

                if (parentTask == null)
                    return false;

                // كل التاسكات تابعة لنفس الـ Parent
                if (tasks.Any(t => t.ParentTaskId != parentTask.Id))
                    return false;

                // توحيد IsOH بناء على Parent
                foreach (var task in tasks)
                    task.Metadata.IsOH = parentTask.Metadata.IsOH;
            }
            else
            {
                bool calcExists = await context.Calculations
                    .AnyAsync(x => x.Id == targetCalcId, cancellationToken);

                if (!calcExists)
                    return false;
            }

            var entities = tasks
                .Select(t => TaskMapper.MapToTaskEntity(t, targetCalcId))
                .ToList();

            context.Tasks.AddRange(entities);
            await context.SaveChangesAsync(cancellationToken);

            var entityIds = entities.Select(e => e.Id).ToList();
            await LoadTaskNavigationAsync(entityIds, cancellationToken);

            entities = TaskExtention.FlattenTasks(entities);
            await NotifyTasks(OperationType.AddRange, targetCalcId, entities, cancellationToken);

            return true;
        }

        // -----------------------------------------------------
        // Cut (TODO: implement domained behaviour)
        // -----------------------------------------------------
        public async Task<bool> CutAsync(
            int sourceCalcId,
            int targetCalcId,
            int? parentTaskId,
            IReadOnlyList<ResourceTaskItemDTO> items,
            double order = 100,
            bool isOH = false,
            CancellationToken cancellationToken = default)
        {
            if (items is null || items.Count == 0)
                return false;

            // في الكوماند القديمة: إذا 0 نعتبره null
            if (parentTaskId == 0)
                parentTaskId = null;

            double? maxOrder = 0;

            // --------------------------------------
            // حساب أعلى SortOrder في الهدف (TaskParent أو Calculation)
            // وتحديد IsOH من الـ parent إذا وجد
            // --------------------------------------
            if (parentTaskId.HasValue && parentTaskId > 0)
            {
                var parent = await context.Tasks
                    .AsNoTracking()
                    .Include(x => x.Tasks)
                    .FirstOrDefaultAsync(
                        x => x.Id == parentTaskId && x.CalculationId == targetCalcId,
                        cancellationToken);

                if (parent == null)
                    return false;

                // نفس منطق CutTaskCommand: نأخذ IsOH من الـ Parent
                isOH = parent.Metadata.IsOH;

                if (parent.Tasks == null || parent.Tasks.Count == 0)
                    maxOrder = null;
                else
                    maxOrder = parent.Tasks.Max(x => (double?)x.SortOrder);
            }
            else
            {
                var calc = await context.Calculations
                    .AsNoTracking()
                    .Include(c => c.Tasks)
                    .FirstOrDefaultAsync(x => x.Id == targetCalcId, cancellationToken);

                if (calc == null)
                    return false;

                if (calc.Tasks == null || calc.Tasks.Count == 0)
                    maxOrder = null;
                else
                    maxOrder = calc.Tasks.Max(x => (double?)x.SortOrder);
            }

            if (maxOrder == null)
                maxOrder = 0;
            else
                maxOrder += 100;

            var movedEntities = new List<TaskEntity>();

            // --------------------------------------
            // لكل عنصر (Task) نطبق نفس منطق CutTaskCommand
            // --------------------------------------
            foreach (var item in items)
            {
                // نفس الـ Calculation → فقط نغيّر Parent / IsOH
                if (sourceCalcId == targetCalcId)
                {
                    var task = await context.Tasks
                        .FirstOrDefaultAsync(
                            x => x.Id == item.Id && x.CalculationId == sourceCalcId,
                            cancellationToken);

                    if (task == null)
                        continue;

                    task.Metadata.IsOH = isOH;
                    TaskExtention.SetNetCalcId(task);
                    task.ParentTaskId = parentTaskId;

                    // في الكود الأصلي لم تغيّر SortOrder هنا، فنحافظ على نفس السلوك
                    context.Tasks.Update(task);
                    movedEntities.Add(task);
                }
                else
                {
                    // نقل بين Calculation مختلفة → نحتاج الشجرة كاملة + الموارد
                    var tasks = await context.Tasks
                        .FromSqlRaw("EXEC GetRecursiveTasks {0}", item.Id)
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                    if (tasks == null || tasks.Count == 0)
                        continue;

                    var taskIds = tasks.Select(t => t.Id).ToList();

                    var resources = await context.Resources
                        .Where(r => taskIds.Contains(r.TaskId))
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                    foreach (var t in tasks)
                    {
                        t.Resources = resources.Where(r => r.TaskId == t.Id).ToList();
                    }

                    // الجذر (Root) هو التاسك اللي ParentTaskId == null
                    var root = tasks.FirstOrDefault(x => x.ParentTaskId == null);
                    if (root == null)
                        continue;

                    TaskExtention.BuildTaskHierarchy(tasks);
                    root = TaskExtention.Reset(root);

                    // حذف الأصل من الـ Calculation القديمة
                    var originalRoot = await context.Tasks
                        .FirstOrDefaultAsync(
                            x => x.Id == item.Id && x.CalculationId == sourceCalcId,
                            cancellationToken);

                    if (originalRoot != null)
                    {
                        context.Tasks.Remove(originalRoot);
                        await context.SaveChangesAsync(cancellationToken);
                    }

                    // إعداد خصائص الجذر في الـ Calculation الجديدة
                    root.ParentTaskId = parentTaskId;

                    if (root.CalculationId != targetCalcId &&
                        !string.IsNullOrEmpty(root.Metadata.QuantityParam))
                    {
                        root.Metadata.QuantityParam = PMValuesConst.FixedQ;
                    }

                    root.Metadata.Quantity = item.Value;
                    root.CalculationId = targetCalcId;
                    root.Metadata.IsOH = isOH;

                    root.SortOrder = maxOrder.Value;
                    maxOrder += 100;

                    TaskExtention.SetNetCalcId(root);

                    await context.Tasks.AddAsync(root, cancellationToken);
                    movedEntities.Add(root);
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            // تحميل الـ Navigation المطلوبة (Status / Opportunity)
            foreach (var item in movedEntities)
            {
                await context.Tasks.Entry(item).Reference(p => p.Status).LoadAsync(cancellationToken);
                await context.Tasks.Entry(item).Reference(p => p.Opportunity).LoadAsync(cancellationToken);
            }

            // تجهيز DTOs للإرسال عبر الـ Hub
            var listHub = movedEntities
                .Select(x => x.MapToTaskListDTO())
                .ToList();

            // نفس منطق الإشعارات في CutTaskCommand
            if (sourceCalcId == targetCalcId)
            {
                await notification.SendNotificationAsync(
                    targetCalcId.ToString(),
                    ObjectTypHub.task,
                    OperationType.MoveRange,
                    Tuple.Create(listHub, movedEntities.Select(x => x.Id)));
            }
            else
            {
                await notification.SendNotificationAsync(
                    sourceCalcId.ToString(),
                    ObjectTypHub.task,
                    OperationType.RemoveRange,
                    movedEntities.Select(x => x.Id));

                await notification.SendNotificationAsync(
                    targetCalcId.ToString(),
                    ObjectTypHub.task,
                    OperationType.AddRange,
                    listHub);
            }

            return true;
        }


        // -----------------------------------------------------
        // Delete tasks (with children via stored procedure)
        // -----------------------------------------------------
        public async Task<bool> DeleteAsync(
            IEnumerable<int> taskIds,
            int calcId,
            CancellationToken cancellationToken = default)
        {
            if (taskIds is null)
                return false;

            var deletedTaskIds = new List<int>();

            foreach (var id in taskIds)
            {
                var tasksToDelete = await GetTaskWithChildrenAsync(id, cancellationToken);
                if (tasksToDelete is not null && tasksToDelete.Any())
                {
                    deletedTaskIds.Add(id);
                    context.Tasks.RemoveRange(tasksToDelete);
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            await notification.SendNotificationAsync(
                calcId.ToString(),
                ObjectTypHub.task,
                OperationType.RemoveRange,
                deletedTaskIds);

            return true;
        }

        // -----------------------------------------------------
        // Change SortOrder of a single task
        // -----------------------------------------------------
        public async Task<bool> NewOrderAsync(
            int taskId,
            double newOrder,
            CancellationToken cancellationToken = default)
        {
            var taskWithCalcId = await context.Tasks
                .Where(x => x.Id == taskId)
                .Select(x => new { Task = x, CalcId = x.CalculationId })
                .FirstOrDefaultAsync(cancellationToken);

            if (taskWithCalcId == null)
                return false;

            taskWithCalcId.Task.SortOrder = newOrder;
            return await UpdateTaskAsync(taskWithCalcId.CalcId, taskWithCalcId.Task, cancellationToken);
        }

        // -----------------------------------------------------
        // Update task from DTO
        // -----------------------------------------------------
        public async Task<bool> UpdateAsync(
            int taskId,
            TaskPostDTO dto,
            CancellationToken cancellationToken = default)
        {
            var taskWithCalcId = await context.Tasks
                .Where(x => x.Id == taskId)
                .Select(x => new { Task = x, CalcId = x.CalculationId })
                .FirstOrDefaultAsync(cancellationToken);

            if (taskWithCalcId == null)
                return false;

            var task = taskWithCalcId.Task;

            task.Name = dto.Name;
            task.StatusId = dto.StatusId;
            task.OpportunityId = dto.OpportunityId;
            task.Metadata = dto.Metadata;
            task.Metadata.QuantityParam = dto.Metadata.QuantityParam;

            // منطق خاص: لو تغيّر النوع إلى CodeName و فيه Resources → نمنع التغيير
            if (dto.Metadata.Type != task.Metadata.Type &&
                dto.Metadata.Type == TaskType.CodeName)
            {
                bool hasResources = await context.Resources
                    .AnyAsync(x => x.TaskId == task.Id, cancellationToken);

                if (hasResources)
                    return false;

                task.StatusId = null;
                task.OpportunityId = null;
                task.Metadata = new TaskData
                {
                    IsOH = dto.Metadata.IsOH,
                    Type = TaskType.CodeName,
                    Quantity = null,
                };
            }

            return await UpdateTaskAsync(taskWithCalcId.CalcId, task, cancellationToken);
        }

        // -----------------------------------------------------
        // Helper: save + reload + notify
        // -----------------------------------------------------
        private async Task<bool> UpdateTaskAsync(
            int calcId,
            TaskEntity task,
            CancellationToken cancellationToken)
        {
            context.Tasks.Update(task);
            await context.SaveChangesAsync(cancellationToken);

            await LoadTaskNavigationAsync([task.Id], cancellationToken);

            var taskDto = task.MapToTaskListDTO();

            await notification.SendNotificationAsync(
                calcId.ToString(),
                ObjectTypHub.task,
                OperationType.Update,
                taskDto);

            return true;
        }

        // -----------------------------------------------------
        // Helper: get task + all children via stored proc
        // -----------------------------------------------------
        private async Task<List<TaskEntity>> GetTaskWithChildrenAsync(
            int rootTaskId,
            CancellationToken cancellationToken)
        {
            return await context.Tasks
                .FromSqlRaw("EXEC GetRecursiveTasks {0}", rootTaskId)
                .IgnoreQueryFilters()
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        // -----------------------------------------------------
        // Helper: get max SortOrder in calc / under parent
        // -----------------------------------------------------
        private async Task<double> GetMaxOrderAsync(
            int calculationId,
            int? parentTaskId,
            CancellationToken ct)
        {
            if (parentTaskId.HasValue)
            {
                var max = await context.Tasks
                    .Where(x => x.ParentTaskId == parentTaskId)
                    .Select(x => (double?)x.SortOrder)
                    .MaxAsync(ct);

                return max ?? 0;
            }
            else
            {
                var max = await context.Tasks
                    .Where(x => x.CalculationId == calculationId && x.ParentTaskId == null)
                    .Select(x => (double?)x.SortOrder)
                    .MaxAsync(ct);

                return max ?? 0;
            }
        }

        // -----------------------------------------------------
        // Helper: clone subtree + resources to another calc
        // -----------------------------------------------------
        public async Task<List<TaskEntity>?> CloneTasksAsync(
            int rootTaskId,
            int targetCalcId,
            int sourceCalcId,
            bool isOH,
            bool deleteOriginal,
            int? parentTaskId,
            double order,
            CancellationToken cancellationToken)
        {
            var tasks = await GetTaskWithChildrenAsync(rootTaskId, cancellationToken);
            if (tasks == null || tasks.Count == 0)
                return null;

            var taskIds = tasks.Select(t => t.Id).ToList();

            var resources = await context.Resources
                .Where(r => taskIds.Contains(r.TaskId))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            foreach (var task in tasks)
                task.Resources = [.. resources.Where(r => r.TaskId == task.Id)];

            var rootTask = tasks.FirstOrDefault(t => t.Id == rootTaskId);
            if (rootTask == null)
                return null;

            TaskExtention.BuildTaskHierarchy(tasks);
            TaskExtention.SetCalculationIdRecursive(tasks, isOH, targetCalcId, sourceCalcId);

            if (parentTaskId.HasValue)
                rootTask.ParentTaskId = parentTaskId;

            // ✅ استعمل الـ order اللي تم تمريره
            rootTask.SortOrder = order;

            await context.Tasks.AddAsync(rootTask, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            await LoadTaskReferencesAsync(tasks, cancellationToken);

            // ملاحظة: deleteOriginal يمكن استخدامه لاحقاً في CutAsync لحذف النسخة الأصلية
            return tasks;
        }

        // -----------------------------------------------------
        // Helper: load navigation props (Status / Opportunity)
        // -----------------------------------------------------
        private async Task LoadTaskNavigationAsync(
            List<int> taskIds,
            CancellationToken cancellationToken)
        {
            if (taskIds == null || taskIds.Count == 0)
                return;

            await context.Tasks
                .Where(t => taskIds.Contains(t.Id))
                .Include(t => t.Status)
                .Include(t => t.Opportunity)
                .LoadAsync(cancellationToken);
        }

        // -----------------------------------------------------
        // Helper: load task references + resources
        // -----------------------------------------------------
        public async Task LoadTaskReferencesAsync(
            List<TaskEntity> tasks,
            CancellationToken cancellationToken)
        {
            if (tasks == null || tasks.Count == 0)
                return;

            List<int> taskIds = [.. tasks.Select(t => t.Id)];

            await LoadTaskNavigationAsync(taskIds, cancellationToken);

            List<ResourceEntity> allResources = await context.Resources
                .Where(r => taskIds.Contains(r.TaskId))
                .Include(r => r.Status)
                .Include(r => r.Account)
                .Include(r => r.ResourceType)
                .Include(r => r.ResourceSort)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var resourceLookup = allResources
                .GroupBy(r => r.TaskId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var task in tasks)
                task.Resources = resourceLookup.TryGetValue(task.Id, out var resList)
                    ? resList
                    : new List<ResourceEntity>();
        }

        // -----------------------------------------------------
        // Helper: notify hub about list of tasks
        // -----------------------------------------------------
        private async Task NotifyTasks(
            OperationType operationType,
            int calcId,
            List<TaskEntity> tasks,
            CancellationToken cancellationToken)
        {
            var dtos = tasks.MapToTaskListDTOs();

            await notification.SendNotificationAsync(
                calcId.ToString(),
                ObjectTypHub.task,
                operationType,
                dtos);
        }
    }
}
