using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.ViewModel;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Client.Shared.Calculation
{
    public static class CalculationStaticFun
    {
     public static void Reset(List<TaskListMVVM> tasks,bool allFalse = false)
        {
            foreach (var task in tasks)
            {
                task.FilterVisible = allFalse;
                if (task.Resources != null) foreach (var res in task.Resources)
                        res.FilterVisible = allFalse;
            }
        }

        public static void Filter(FilterVM filter,List<TaskListMVVM> tasks)
        {
            if(filter.Code.Count == 0 && filter.Name.Count == 0 && filter.Unit.Count == 0 &&
                filter.Status.Count == 0 && filter.Account.Count == 0 && filter.ResourceTypeSystem.Count == 0 &&
                filter.Resource.Count == 0 && filter.ResourceSort.Count == 0)
                Reset(tasks, true);
            else
            {
                Reset(tasks);
            foreach (var task in tasks)
            {
                 Filter(filter,task);
            }
            foreach (var task in tasks)
            {
                sss(task, tasks);
            }
            }

        }

        static void sss(TaskListMVVM task, List<TaskListMVVM> tasks)
        {
            if (!task.FilterVisible)
            {
                var childreen = tasks.Where(x => x.TaskId == task.Id);

                if (childreen != null) { 
            if (task.Tasks.Any(x => x.FilterVisible)) task.FilterVisible = true;

                else foreach (var item in childreen)
                    {
                        sss(item, tasks);
                    }
                } 
            }
        }
        static bool FilterStatus(List<string> items, FilterType type, string str)
        {
            if (type == FilterType.equals && !string.IsNullOrEmpty(str) && items.Any(f => f.ToLower() == str.ToLower()))
                return false;
            else if (type == FilterType.noEquals && (string.IsNullOrEmpty(str) || items.Any(f => !f.Equals(str, System.StringComparison.CurrentCultureIgnoreCase))))
                return false;
            return true;
        }
        static bool Filter(List<string> items, FilterType type, string str)
        {
            bool isSuccess = true;
            if (type == FilterType.equals && items.Any(f => f.Equals(str, System.StringComparison.CurrentCultureIgnoreCase)))
                return isSuccess;
            else if (type == FilterType.noEquals && items.Any(f => !f.Equals(str, System.StringComparison.CurrentCultureIgnoreCase)))
                return isSuccess;
            else if (type == FilterType.contains && items.Any(f => str.Contains(f, System.StringComparison.CurrentCultureIgnoreCase)))
                return isSuccess;
            else if (type == FilterType.doesNotContains && ( !items.Any(f => str.Contains(f, System.StringComparison.CurrentCultureIgnoreCase))))
                return isSuccess;
            else if (type == FilterType.startsWith  && items.Any(f => str.StartsWith(f, System.StringComparison.CurrentCultureIgnoreCase)))
                return isSuccess;
            else if (type == FilterType.endsWith && items.Any(f => str.ToLower().EndsWith(f.ToLower())))
                return isSuccess;
            return !isSuccess;
        }

        static void Filter(this FilterVM filter, ResourceListMVVM resource)
        {
            if (filter.Name.Count != 0 && !resource.FilterVisible)
                resource.FilterVisible = Filter(filter.Name, filter.NameFilterType, resource.Name);
            if (filter.Unit.Count != 0 && !resource.FilterVisible)
                resource.FilterVisible = Filter(filter.Unit, filter.UnitFilterType, resource.Unit);
            if (filter.Account.Count != 0 && !resource.FilterVisible)
                resource.FilterVisible = Filter(filter.Account, filter.AccountFilterType, resource.AccountCode);
            if (filter.ResourceTypeSystem.Count != 0 && !resource.FilterVisible)
            {
                if (filter.ResourceTypeFilterTypeSystem == FilterType.equals && !string.IsNullOrEmpty(resource.ResName) && filter.ResourceTypeSystem.Any(f => resource.ResType == f))
                    resource.FilterVisible = true;
                if (filter.ResourceTypeFilterTypeSystem == FilterType.noEquals && (string.IsNullOrEmpty(resource.ResName) || !filter.ResourceTypeSystem.Any(f => resource.ResType == f)))
                    resource.FilterVisible = true;
            }
            if (filter.Resource.Count != 0 && !resource.FilterVisible)
                resource.FilterVisible = Filter(filter.Resource, filter.ResourceFilterType, resource.ResName);

            if (filter.ResourceSort.Count != 0 && !resource.FilterVisible)
                resource.FilterVisible = Filter(filter.ResourceSort, filter.ResourceSortFilterType, resource.Sort);
        }
        static void Filter(this FilterVM filter, TaskListMVVM task)
        {
            if (filter.Status.Count != 0 && !task.FilterVisible)
                task.FilterVisible = FilterStatus(filter.Status, filter.StatusFilterType, task.Status);
            if (filter.Code.Count != 0 && !task.FilterVisible)
                task.FilterVisible = Filter(filter.Code, filter.CodeFilterType, task.Metadata.Code);
            if (filter.Name.Count != 0 && !task.FilterVisible)
                task.FilterVisible = Filter(filter.Name, filter.NameFilterType, task.Name);
            if (filter.Unit.Count != 0 && !task.FilterVisible)
                task.FilterVisible = Filter(filter.Unit, filter.UnitFilterType, task.Metadata.Unit);

            if (!task.FilterVisible)
            {
            foreach (var res in task.Resources) filter.Filter(res);
            if(task.Resources.Any(x=>x.FilterVisible)) task.FilterVisible = true;
            }

        }
    }
}
