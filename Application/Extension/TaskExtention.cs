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
                task.TaskId = null;
                task.Data.IsOH = oh;

                if (calcId != oldcalcId)
                    task.OpportunityId = null;
                foreach (var res in task.Resources ?? Enumerable.Empty<ResourceEntity>())
                {
                    if (calcId != oldcalcId)
                    {
                        res.OpportunityId = null;
                        res.OfferId = null;
                        res.Offers = null;
                    }
                    res.Id = 0;
                    res.TaskId = 0;
                }
            }
        }
        public static void BuildTaskHierarchy(List<TaskEntity> all)
        {
            var lookup = all.Where(x => x.TaskId != null).GroupBy(x => x.TaskId).ToDictionary(g => g.Key, g => g.ToList());
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
            task.Tasks?.ForEach(t =>
            {
                t.CalculationId = task.CalculationId;
                t.Data.IsOH = task.Data.IsOH;
                SetNetCalcId(t);
            });
        }
        public static TaskEntity Reset(TaskEntity task)
        {
            task.Id = 0;
            task.TaskId = null;
            task.OpportunityId = null;
            task.CalculationId = 0;
            task.Opportunity = null;
            task.Task = null;
            task.Status = null;

            if (task.Resources != null) for (int i = 0; i < task.Resources.Count; i++)
                    task.Resources[i] = ResourceExtention.Reset(task.Resources.ElementAt(i));
            for (int i = 0; i < task?.Tasks?.Count; i++)
                task.Tasks[i] = Reset(task.Tasks.ElementAt(i));

            return task;
        }
    }
}