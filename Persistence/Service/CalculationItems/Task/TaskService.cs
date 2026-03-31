using Application.Extention;
using Application.Feature.Calculation.Task;
using Application.Interfaces;
using Application.Mapping.CalcItems;
using Domain.Entities.Calculation;
using Persistence.Context;
using Persistence.Factory;
using Persistence.Service.Sql;
using Microsoft.EntityFrameworkCore.Storage;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Exceptions;

namespace Persistence.Service.CalculationItems.Task
{
    public class TaskService(IDbContextFactoryTenant dbFactory, INotificationHub notification) : ITaskService
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
            CancellationToken ct = default)
        {
            if (taskItems is null || taskItems.Count == 0)
                return false;
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var resultDtos = new List<TaskListDTO>();

            // ✅ نحسب الـ Order في الـ Target Calculation (وليس المصدر)
            int order = await GetMaxOrderAsync(targetCalcId, parentTaskId, ct);

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
                    ct: ct);

                if (clonedTasks is not null && clonedTasks.Count != 0)
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
            CancellationToken ct = default)
        {
            if (tasks is null || tasks.Count == 0)
                return false;

            var safeTasks = tasks;
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            int? parentTaskId = safeTasks.FirstOrDefault()?.ParentTaskId;

            if (parentTaskId.HasValue && parentTaskId > 0)
            {
                var parentTask = await context.Tasks
                    .FirstOrDefaultAsync(x => x.Id == parentTaskId && x.CalculationId == targetCalcId, ct);

                if (parentTask == null)
                    return false;

                // كل التاسكات تابعة لنفس الـ Parent
                if (safeTasks.Any(t => t.ParentTaskId != parentTask.Id))
                    return false;

                // توحيد IsOH بناء على Parent
                foreach (var task in safeTasks)
                    task.IsOH = parentTask.IsOH;
            }
            else
            {
                bool calcExists = await context.Calculations
                    .AnyAsync(x => x.Id == targetCalcId, ct);

                if (!calcExists)
                    return false;
            }

            var entities = safeTasks
                .Select(t => TaskMapper.MapToTaskEntity(t, targetCalcId))
                .ToList();

            context.Tasks.AddRange(entities);
            await context.SaveChangesAsync(ct);

            var createdTaskIds = TaskExtention
                .FlattenTasks(entities)
                .Select(e => e.Id)
                .Distinct()
                .ToList();

            var tasksWithNav = await context.Tasks
                .AsNoTracking()
                .Where(t => createdTaskIds.Contains(t.Id))
                .Include(t => t.Status)
                .Include(t => t.Opportunity)
                .Include(t => t.Resources)
                    .ThenInclude(r => r.Account)
                .Include(t => t.Resources)
                    .ThenInclude(r => r.Status)
                .Include(t => t.Resources)
                    .ThenInclude(r => r.Opportunity)
                .Include(t => t.Resources)
                    .ThenInclude(r => r.ResourceSort)
                .Include(t => t.Resources)
                    .ThenInclude(r => r.ResourceType)
                .ToListAsync(ct);

            tasksWithNav = TaskExtention.FlattenTasks(tasksWithNav);

            var dtos = tasksWithNav.MapToTaskListDTOs();

            await notification.SendNotificationAsync(
                targetCalcId.ToString(),
                ObjectTypHub.task,
                OperationType.AddRange,
                dtos);
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
            int order = 100,
            bool isOH = false,
            CancellationToken ct = default)
        {
            if (items is null || items.Count == 0)
                return false;

            // في الكوماند القديمة: إذا 0 نعتبره null
            if (parentTaskId == 0)
                parentTaskId = null;
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            int? maxOrder = 0;

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
                        ct);

                if (parent == null)
                    return false;

                // نفس منطق CutTaskCommand: نأخذ IsOH من الـ Parent
                isOH = parent.Metadata.IsOH;

                if (parent.Tasks == null || parent.Tasks.Count == 0)
                    maxOrder = null;
                else
                    maxOrder = parent.Tasks.Max(x => (int?)x.SortOrder);
            }
            else
            {
                var calc = await context.Calculations
                    .AsNoTracking()
                    .Include(c => c.Tasks)
                    .FirstOrDefaultAsync(x => x.Id == targetCalcId, ct);

                if (calc == null)
                    return false;

                if (calc.Tasks == null || calc.Tasks.Count == 0)
                    maxOrder = null;
                else
                    maxOrder = calc.Tasks.Max(x => (int?)x.SortOrder);
            }

            if (maxOrder == null)
                maxOrder = 100;
            else
                maxOrder += 100;

            var movedEntities = new List<TaskEntity>();
            var removedRootIds = new List<int>();
            IDbContextTransaction? transaction = null;

            if (sourceCalcId != targetCalcId)
                transaction = await context.Database.BeginTransactionAsync(ct);

            try
            {
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
                                ct);

                        if (task == null)
                            continue;

                        task.SetIsOH(isOH);
                        TaskExtention.SetNetCalcId(task);
                        task.SetParentTask(parentTaskId);

                        context.Tasks.Update(task);
                        movedEntities.Add(task);
                        continue;
                    }

                    var tasks = await RecursiveTasksCte.Query(context, item.Id, sourceCalcId)
                        .ToListAsync(ct);

                    if (tasks == null || tasks.Count == 0)
                        continue;

                    var taskIds = tasks.Select(t => t.Id).ToList();

                    var resources = await context.Resources
                        .Where(r => taskIds.Contains(r.TaskId))
                        .AsNoTracking()
                        .ToListAsync(ct);

                    foreach (var t in tasks)
                        t.Resources = [.. resources.Where(r => r.TaskId == t.Id)];

                    var root = tasks.FirstOrDefault(x => x.Id == item.Id);
                    if (root == null)
                        continue;

                    TaskExtention.BuildTaskHierarchy(tasks);
                    root = TaskExtention.Reset(root);

                    var originalRoot = await context.Tasks
                        .FirstOrDefaultAsync(
                            x => x.Id == item.Id && x.CalculationId == sourceCalcId,
                            ct);

                    if (originalRoot != null)
                    {
                        context.Tasks.Remove(originalRoot);
                        removedRootIds.Add(originalRoot.Id);
                    }

                    root.SetParentTask(parentTaskId);

                    if (!string.IsNullOrEmpty(root.Metadata.QuantityParam))
                    {
                        root.UpdateMetadata(m => m.QuantityParam = PMValuesConst.FixedQ);
                    }

                    root.UpdateMetadata(m =>
                    {
                        m.Quantity = item.Value;
                        m.IsOH = isOH;
                    });

                    root.SetCalculation(targetCalcId);
                    root.SetSortOrder(maxOrder.Value);
                    maxOrder += 100;

                    TaskExtention.SetNetCalcId(root);

                    await context.Tasks.AddAsync(root, ct);
                    movedEntities.Add(root);
                }

                await context.SaveChangesAsync(ct);

                if (transaction != null)
                    await transaction.CommitAsync(ct);
            }
            catch
            {
                if (transaction != null)
                    await transaction.RollbackAsync(ct);
                throw;
            }
            finally
            {
                if (transaction != null)
                    await transaction.DisposeAsync();
            }

            // تحميل الـ Navigation المطلوبة (Status / Opportunity)
            foreach (var item in movedEntities)
            {
                await context.Tasks.Entry(item).Reference(p => p.Status).LoadAsync(ct);
                await context.Tasks.Entry(item).Reference(p => p.Opportunity).LoadAsync(ct);
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
                    removedRootIds);

                await notification.SendNotificationAsync(
                    targetCalcId.ToString(),
                    ObjectTypHub.task,
                    OperationType.AddRange,
                    listHub);
            }

            return true;
        }


        // -----------------------------------------------------
        // Delete tasks (new safe path without FromSql composition)
        // -----------------------------------------------------
        public async Task<bool> DeleteAsync(
            IEnumerable<int> taskIds,
            int calcId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (taskIds is null)
                return false;

            var rootIds = taskIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (rootIds.Count == 0)
                return true;

            var existingRootIds = await context.Tasks
                .Where(t => rootIds.Contains(t.Id) && t.CalculationId == calcId)
                .Select(t => t.Id)
                .ToListAsync(ct);

            if (existingRootIds.Count == 0)
                return true;

            var allTaskIdsToDelete = await GetTaskSubtreeIdsForDeleteAsync(
                context,
                calcId,
                existingRootIds,
                ct);

            var tasksToDelete = await context.Tasks
                .Where(t => t.CalculationId == calcId && allTaskIdsToDelete.Contains(t.Id))
                .ToListAsync(ct);

            if (tasksToDelete.Count == 0)
                return true;

            context.Tasks.RemoveRange(tasksToDelete);
            await context.SaveChangesAsync(ct);

            await notification.SendNotificationAsync(
                calcId.ToString(),
                ObjectTypHub.task,
                OperationType.RemoveRange,
                existingRootIds);

            return true;
        }


        private static async Task<HashSet<int>> GetTaskSubtreeIdsForDeleteAsync(
            ShardingSingleDbContext context,
            int calcId,
            IReadOnlyCollection<int> rootIds,
            CancellationToken ct)
        {
            var allTaskIds = new HashSet<int>(rootIds);
            var frontier = rootIds.ToList();

            while (frontier.Count > 0)
            {
                var children = await context.Tasks
                    .Where(t => t.CalculationId == calcId
                        && t.ParentTaskId.HasValue
                        && frontier.Contains(t.ParentTaskId.Value))
                    .Select(t => t.Id)
                    .ToListAsync(ct);

                frontier = children
                    .Where(id => allTaskIds.Add(id))
                    .ToList();
            }

            return allTaskIds;
        }

        // -----------------------------------------------------
        // Change SortOrder of a single task
        // -----------------------------------------------------
        public async Task<bool> NewOrderAsync(
            int taskId,
            int newOrder,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var taskWithCalcId = await context.Tasks
                .Where(x => x.Id == taskId)
                .Select(x => new { Task = x, CalcId = x.CalculationId })
                .FirstOrDefaultAsync(ct);

            if (taskWithCalcId == null)
                return false;

            taskWithCalcId.Task.SetSortOrder(newOrder);
            return await SaveAndNotifyTaskAsync(context, taskWithCalcId.CalcId, taskId, ct);
        }

        // -----------------------------------------------------
        // Update task from DTO
        // -----------------------------------------------------
        public async Task<bool> UpdateAsync(
            int taskId,
            TaskPostDTO dto,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var taskWithCalcId = await context.Tasks
                .Where(x => x.Id == taskId)
                .Select(x => new { Task = x, CalcId = x.CalculationId })
                .FirstOrDefaultAsync(ct);

            if (taskWithCalcId == null)
                return false;

            var task = taskWithCalcId.Task;

            // optimistic concurrency: if UI sends RowVersion, detect stale edits
            if (dto.RowVersion is { Length: > 0 })
                context.Entry(task).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;

            var oldType = task.Metadata.Type;
            var newType = dto.Metadata.Type;

            // منطق خاص: لو تغيّر النوع إلى CodeName و فيه Resources → نمنع التغيير
            if (oldType != newType && newType == TaskType.CodeName)
            {
                bool hasResources = await context.Resources
                    .AnyAsync(x => x.TaskId == task.Id, ct);

                if (hasResources)
                    return false;

                // نجبر DTO إلى وضع CodeName (بدون Status/Opportunity)
                dto.StatusId = null;
                dto.OpportunityId = null;
                dto.Metadata = new TaskMetadata
                {
                    IsOH = dto.Metadata.IsOH,
                    Type = TaskType.CodeName,
                    Quantity = null,
                };
            }

            task.Update(dto);

            return await SaveAndNotifyTaskAsync(context, taskWithCalcId.CalcId, task.Id, ct);

        }
        
        // -----------------------------------------------------
        // Helper: save + reload + notify (with concurrency handling)
        // -----------------------------------------------------
        private async Task<bool> SaveAndNotifyTaskAsync(
            ShardingSingleDbContext context,
            int calcId,
            int taskId,
            CancellationToken ct)
        {
            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyConflictException("Task", taskId);
            }

            // Reload with navs for UI notification
            var taskWithNav = await context.Tasks
                .AsNoTracking()
                .Include(t => t.Status)
                .Include(t => t.Opportunity)
                .Include(t => t.Resources)
                .ThenInclude(r => r.Offers)
                .FirstOrDefaultAsync(t => t.Id == taskId, ct);

            if (taskWithNav is null)
                return false;

            // Load resource lookups (optional, but keeps UI consistent)
            // (EF doesn't allow multiple ThenInclude branches from same Include chain in one go, so we repeat Include)
            taskWithNav.Resources = taskWithNav.Resources ?? [];

            var taskDto = taskWithNav.MapToTaskListDTO();

            await notification.SendNotificationAsync(
                calcId.ToString(),
                ObjectTypHub.task,
                OperationType.Update,
                taskDto);

            return true;
        }


        // -----------------------------------------------------
        // Helper: get max SortOrder in calc / under parent
        // -----------------------------------------------------
        private async Task<int> GetMaxOrderAsync(
            int calculationId,
            int? parentTaskId,
            CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (parentTaskId.HasValue)
            {
                var max = await context.Tasks
                    .Where(x => x.ParentTaskId == parentTaskId)
                     .Select(x => (int?)x.SortOrder)
                    .MaxAsync(ct);

                return (max ?? 0) + 100;
            }
            else
            {
                var max = await context.Tasks
                    .Where(x => x.CalculationId == calculationId && x.ParentTaskId == null)
                     .Select(x => (int?)x.SortOrder)
                    .MaxAsync(ct);

                return (max ?? 0) + 100;
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
            int order,
            CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var tasks = await RecursiveTasksCte.Query(context, rootTaskId, sourceCalcId)
                .ToListAsync(ct);

            if (tasks == null || tasks.Count == 0)
                return null;

            var sourceTaskIds = tasks.Select(t => t.Id).ToList();

            var resources = await context.Resources
                .Where(r => sourceTaskIds.Contains(r.TaskId))
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var task in tasks)
                task.Resources = [.. resources.Where(r => r.TaskId == task.Id)];

            var rootTask = tasks.FirstOrDefault(t => t.Id == rootTaskId);
            if (rootTask == null)
                return null;

            TaskExtention.BuildTaskHierarchy(tasks);
            TaskExtention.SetCalculationIdRecursive(tasks, isOH, targetCalcId, sourceCalcId);

            if (parentTaskId.HasValue)
                rootTask.SetParentTask(parentTaskId);

            // ✅ استعمل الـ order اللي تم تمريره
            rootTask.SetSortOrder(order);

            await context.Tasks.AddAsync(rootTask, ct);
            await context.SaveChangesAsync(ct);
            var newTaskIds = tasks.Select(t => t.Id).ToList();

            //await LoadTaskReferencesAsync(tasks, ct);

            //List<int> taskIds = [.. tasks.Select(t => t.Id)];

            await context.Tasks
    .Where(t => newTaskIds.Contains(t.Id))
    .Include(t => t.Status)
    .Include(t => t.Opportunity)
    .LoadAsync(ct);
            List<ResourceEntity> allResources = await context.Resources
                .Where(r => newTaskIds.Contains(r.TaskId))
                .Include(r => r.Status)
                .Include(r => r.Account)
                .Include(r => r.ResourceType)
                .Include(r => r.ResourceSort)
                .AsNoTracking()
                .ToListAsync(ct);

            var resourceLookup = allResources
                .GroupBy(r => r.TaskId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var task in tasks)
                task.Resources = resourceLookup.TryGetValue(task.Id, out var resList)
                    ? resList
                    : [];
            // ملاحظة: deleteOriginal يمكن استخدامه لاحقاً في CutAsync لحذف النسخة الأصلية
            return tasks;
        }
    }
}
