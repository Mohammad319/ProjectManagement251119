using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.ViewModel;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Client.Shared.Calculation
{
    public static class CalculationStaticFun
    {
        public static void Reset(List<TaskListMVVM> tasks, bool allFalse = false)
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                task.Ui.FilterVisible = allFalse;
                if (task.Resources != null)
                {
                    for (int r = 0; r < task.Resources.Count; r++)
                    {
                        var res = task.Resources[r];
                        res.Ui.FilterVisible = allFalse;
                    }
                }
            }
        }

        public static void Filter(FilterVM filter, List<TaskListMVVM> tasks)
        {
            if (tasks == null || tasks.Count == 0)
                return;

            if (!HasFilters(filter))
            {
                Reset(tasks, true);
                return;
            }

            Reset(tasks);

            HashSet<int> taskIds = [.. tasks.Select(x => x.Id)];

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task.TaskId is null || !taskIds.Contains(task.TaskId.Value))
                    ApplyFilter(filter, task);
            }
        }

        private static bool HasFilters(FilterVM filter) =>
            filter.Code.Count != 0 || filter.Name.Count != 0 || filter.Unit.Count != 0 ||
            filter.Status.Count != 0 || filter.Account.Count != 0 || filter.ResourceTypeSystem.Count != 0 ||
            filter.Resource.Count != 0 || filter.ResourceSort.Count != 0;

        private static bool FilterStatus(List<string> items, FilterType type, string str)
        {
            if (type == FilterType.equals && !string.IsNullOrEmpty(str) && items.Any(f => f.ToLower() == str.ToLower()))
                return false;
            if (type == FilterType.noEquals && (string.IsNullOrEmpty(str) || items.Any(f => !f.Equals(str, System.StringComparison.CurrentCultureIgnoreCase))))
                return false;
            return true;
        }

        private static bool Filter(List<string> items, FilterType type, string str)
        {
            bool isSuccess = true;
            if (type == FilterType.equals && items.Any(f => f.Equals(str, System.StringComparison.CurrentCultureIgnoreCase)))
                return isSuccess;
            if (type == FilterType.noEquals && items.Any(f => !f.Equals(str, System.StringComparison.CurrentCultureIgnoreCase)))
                return isSuccess;
            if (type == FilterType.contains && items.Any(f => str.Contains(f, System.StringComparison.CurrentCultureIgnoreCase)))
                return isSuccess;
            if (type == FilterType.doesNotContains && !items.Any(f => str.Contains(f, System.StringComparison.CurrentCultureIgnoreCase)))
                return isSuccess;
            if (type == FilterType.startsWith && items.Any(f => str.StartsWith(f, System.StringComparison.CurrentCultureIgnoreCase)))
                return isSuccess;
            if (type == FilterType.endsWith && items.Any(f => str.EndsWith(f, System.StringComparison.OrdinalIgnoreCase)))
                return isSuccess;
            return !isSuccess;
        }

        private static bool MatchesResource(FilterVM filter, ResourceListMVVM resource)
        {
            if (filter.Name.Count != 0 && Filter(filter.Name, filter.NameFilterType, resource.Name))
                return true;

            if (filter.Unit.Count != 0 && Filter(filter.Unit, filter.UnitFilterType, resource.Unit))
                return true;

            if (filter.Account.Count != 0 && Filter(filter.Account, filter.AccountFilterType, resource.AccountCode ?? string.Empty))
                return true;

            if (filter.ResourceTypeSystem.Count != 0)
            {
                if (filter.ResourceTypeFilterTypeSystem == FilterType.equals &&
                    !string.IsNullOrEmpty(resource.ResName) &&
                    filter.ResourceTypeSystem.Any(f => resource.ResType == f))
                {
                    return true;
                }

                if (filter.ResourceTypeFilterTypeSystem == FilterType.noEquals &&
                    (string.IsNullOrEmpty(resource.ResName) || !filter.ResourceTypeSystem.Any(f => resource.ResType == f)))
                {
                    return true;
                }
            }

            if (filter.Resource.Count != 0 && Filter(filter.Resource, filter.ResourceFilterType, resource.ResName ?? string.Empty))
                return true;

            if (filter.ResourceSort.Count != 0 && Filter(filter.ResourceSort, filter.ResourceSortFilterType, resource.Sort ?? string.Empty))
                return true;

            return false;
        }

        private static bool MatchesTask(FilterVM filter, TaskListMVVM task)
        {
            if (filter.Status.Count != 0 && FilterStatus(filter.Status, filter.StatusFilterType, task.Status))
                return true;

            if (filter.Code.Count != 0 && Filter(filter.Code, filter.CodeFilterType, task.Metadata.Code))
                return true;

            if (filter.Name.Count != 0 && Filter(filter.Name, filter.NameFilterType, task.Name))
                return true;

            if (filter.Unit.Count != 0 && Filter(filter.Unit, filter.UnitFilterType, task.Unit))
                return true;

            return false;
        }

        private static bool ApplyFilter(FilterVM filter, TaskListMVVM task)
        {
            bool taskVisible = MatchesTask(filter, task);
            bool hasVisibleResource = false;

            if (task.Resources is not null)
            {
                for (int i = 0; i < task.Resources.Count; i++)
                {
                    var resource = task.Resources[i];
                    resource.Ui.FilterVisible = MatchesResource(filter, resource);

                    if (resource.Ui.FilterVisible)
                        hasVisibleResource = true;
                }
            }

            bool hasVisibleChild = false;
            if (task.Tasks is not null)
            {
                for (int i = 0; i < task.Tasks.Count; i++)
                {
                    if (ApplyFilter(filter, task.Tasks[i]))
                        hasVisibleChild = true;
                }
            }

            task.Ui.FilterVisible = taskVisible || hasVisibleResource || hasVisibleChild;
            return task.Ui.FilterVisible;
        }
    }
}
