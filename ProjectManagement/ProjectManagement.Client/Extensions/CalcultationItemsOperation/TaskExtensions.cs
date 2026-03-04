using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Base.Calculation;

namespace ProjectManagement.Client.Extensions.CalcultationItemsOperation
{
    public static class TaskExtensions
    {
        public static string Style(this TaskListMVVM task, string color, bool parentActive)
        {
            if (SelectedData.ExistItem(CalculationItemType.task, task.Id))
                return CSS.SelectedItem;

            return task.Metadata.IsActive && parentActive ? $"background-color:{color};"
                : $"background-color:{color};color:rgba(180, 180, 180, 0.5);";
        }


        public static bool AddTasks(this CalculationMVVM calculation, List<TaskListMVVM> tasks)
        {
            if (tasks == null) return false;

            calculation.Tasks ??= [];
            calculation.Tasks.InsertRange(0, tasks);

            foreach (var task in tasks)
            {
                task.Tasks = [.. calculation.Tasks.Where(x => x.TaskId == task.Id)];
                task.Tasks ??= [];
                task.Resources ??= [];
            }
            if (!tasks.Any(x => x.TaskId == null)) calculation.BuildTaskHierarchy();
            return true;
        }
        public static bool RemoveTasks(this CalculationMVVM calculation, List<int> taskIds)
        {
            if (taskIds == null || calculation?.Tasks == null) return false;

            foreach (var id in taskIds)
            {
                var task = calculation.Tasks.FirstOrDefault(x => x.Id == id);
                if (task == null) continue;

                calculation.Tasks.Remove(task);

                if (task.TaskId.HasValue && task.TaskId > 0)
                {
                    var parent = calculation.Tasks.FirstOrDefault(x => x.Id == task.TaskId.Value);
                    parent?.Tasks?.Remove(task);
                }
            }

            return true;
        }


        public static void CalcVaribles(
            this TaskListMVVM task,
            Dictionary<string, QuanityListDTO> qIndex,
            decimal? parentQuantity)
        {
            if (task.Metadata.Type == TaskType.CodeName)
            {
                task.Metadata.Quantity = null;
            }
            else if (!string.IsNullOrEmpty(task.Metadata.QuantityParam))
            {
                if (qIndex.TryGetValue(task.Metadata.QuantityParam, out var param))
                    task.Metadata.Quantity = param.Quantity;
                else
                    task.Metadata.QuantityParam = ConstValues.FixedQ;
            }
            else
            {
                task.Metadata.Quantity = task.Metadata.ChangeFactor1 * task.Metadata.ChangeFactor2 * (parentQuantity ?? 0m);
            }

            if (task.Tasks is not null)
            {
                var nextParent = task.Metadata.Quantity ?? parentQuantity;
                for (int i = 0; i < task.Tasks.Count; i++)
                    task.Tasks[i].CalcVaribles(qIndex, nextParent);
            }
        }
    }
}
