using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Extention
{
    public static class TaskExtention
    {
        public static List<TaskEntity> FlattenTasks(IEnumerable<TaskEntity> tasks)
            => tasks.SelectMany(t => new[] { t }.Concat(FlattenTasks(t.Tasks ?? Enumerable.Empty<TaskEntity>()))).ToList();

        public static void SetCalculationIdRecursive(List<TaskEntity> tasks, bool oh, int calcId, int oldcalcId)
        {
            foreach (var task in tasks ?? Enumerable.Empty<TaskEntity>())
            {
                task.SetCalculation(calcId);
                task.ResetIdentityForClone();
                task.SetIsOH(oh);

                if (calcId != oldcalcId)
                    task.ClearOpportunity();

                foreach (var resource in task.Resources ?? Enumerable.Empty<ResourceEntity>())
                {
                    if (calcId != oldcalcId)
                        resource.ClearCrossCalculationState(resetQuantityParam: false);

                    resource.ResetIdentityForClone();
                }
            }
        }

        public static void BuildTaskHierarchy(List<TaskEntity> all)
        {
            var lookup = all
                .Where(x => x.ParentTaskId.HasValue)
                .GroupBy(x => x.ParentTaskId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var task in all)
            {
                task.SetChildTasks(lookup.TryGetValue(task.Id, out var children)
                    ? children
                    : []);
            }
        }

        public static void SetNetCalcId(TaskEntity task)
        {
            foreach (var child in task.Tasks)
            {
                child.SetCalculation(task.CalculationId);
                child.SetIsOH(task.IsOH);
                SetNetCalcId(child);
            }
        }

        public static TaskEntity Reset(TaskEntity task)
        {
            task.ResetIdentityForClone();
            task.ClearOpportunity();

            if (task.Resources != null)
                task.SetResources(task.Resources.Select(ResourceExtention.Reset).ToList());

            task.SetChildTasks([.. task.Tasks.Select(Reset)]);
            return task;
        }
    }
}
