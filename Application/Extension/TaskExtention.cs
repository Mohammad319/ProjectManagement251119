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
                task.CalculationId = calcId;
                task.Id = 0;
                task.ParentTaskId = null;
                task.Metadata.IsOH = oh;

                if (calcId != oldcalcId)
                    task.OpportunityId = null;
                foreach (var res in task.Resources ?? Enumerable.Empty<ResourceEntity>())
                {
                    if (calcId != oldcalcId)
                    {
                        res.OpportunityId = null;
                        res.PrimaryOfferId = null;
                        res.Offers = null;
                    }
                    res.Id = 0;
                    res.TaskId = 0;
                }
            }
        }
        public static void BuildTaskHierarchy(List<TaskEntity> all)
        {
            var lookup = all.Where(x => x.ParentTaskId != null).GroupBy(x => x.ParentTaskId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var task in all)
            {
                if (lookup.TryGetValue(task.Id, out var children))
                    task.Tasks = children;
                else
                    task.Tasks = [];
            }
        }
        public static void SetNetCalcId(TaskEntity task)
        {
            foreach (var t in task.Tasks)
            {
                t.CalculationId = task.CalculationId;
                t.Metadata.IsOH = task.Metadata.IsOH;
                SetNetCalcId(t);
            }
        }
        public static TaskEntity Reset(TaskEntity task)
        {
            task.Id = 0;
            task.ParentTaskId = null;
            task.OpportunityId = null;
            task.CalculationId = 0;
            task.Opportunity = null;
            task.ParentTask = null;
            task.Status = null;

            //if (task.Resources != null) for (int i = 0; i < task.Resources.Count; i++)
            //        task.Resources[i] = ResourceExtention.Reset(task.Resources.ElementAt(i));
            if (task.Resources != null)
            {
                task.Resources = task.Resources
                    .Select(r => ResourceExtention.Reset(r))
                    .ToList();
            }


            //for (int i = 0; i < task?.Tasks?.Count; i++)
            //    task.Tasks[i] = Reset(task.Tasks.ElementAt(i));
            //foreach (var child in task.Tasks)Reset(child);
            task.Tasks = [.. task.Tasks.Select(t => Reset(t))];

            return task;
        }
    }
}