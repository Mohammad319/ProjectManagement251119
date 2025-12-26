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

            return task.Data.IsActive && parentActive ? $"background-color:{color};"
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
            if (taskIds == null) return false;

            foreach (var id in taskIds)
            {
                var task = calculation?.Tasks?.FirstOrDefault(x => x.Id == id);
                if (task == null) continue;

                calculation.Tasks.Remove(task);

                if (task.TaskId.HasValue && task.TaskId > 0)
                {
                    var parent = calculation.Tasks.FirstOrDefault(x => x.Id == task.TaskId);
                    parent?.Tasks?.Remove(task);
                }
            }

            return true;
        }


        public static void CalcVaribles(this TaskListMVVM task, List<QuanityListDTO> quantityList, double? parentQuantity)
        {
            if (task.Data.Type == TaskType.CodeName) task.Data.Quantity = null;
            else if (!string.IsNullOrEmpty(task.Data.QuantityParam))
            {
                QuanityListDTO param = quantityList.FirstOrDefault(x => x.Name == task.Data.QuantityParam);
                if (param != null) task.Data.Quantity = param.Quantity;
                else task.Data.QuantityParam = ConstValues.FixedQ;
            }
            else task.Data.Quantity = task.Data.ChangeFactor1 * task.Data.ChangeFactor2 * (parentQuantity ?? 0);
            if (task.Tasks != null) foreach (var subTask in task.Tasks)
                    subTask.CalcVaribles(quantityList, task.Data.Quantity ?? parentQuantity);
        }
    }
}
