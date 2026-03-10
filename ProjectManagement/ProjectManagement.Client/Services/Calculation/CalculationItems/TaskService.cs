using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Shared.DTO.Hub;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Services.Calculation.CalculationItems
{
    public class TaskService(
        ITaskRepository Repo,
        FolderState CalcContainer,
        IStorageRepository storage,
        MhdServices Mhd,
        ContextMenuService ContextMenuService,
        IContextMenuBuilderService context,
        DialogService dialogService)
    {
        private static bool TaskAffectsCalculation(TaskListMVVM oldT, TaskListMVVM newT)
        {
            if (oldT.Metadata?.Quantity != newT.Metadata?.Quantity) return true;
            if (oldT.Metadata?.ChangeFactor1 != newT.Metadata?.ChangeFactor1) return true;
            if (oldT.Metadata?.ChangeFactor2 != newT.Metadata?.ChangeFactor2) return true;
            return false;
        }

        public async Task ContextMenu(TaskListMVVM task)
        {
            List<MenuItem> list = context.BuildTaskContextMenu(task, () => Remove(task), async () => await Duplicate(task));
            await ContextMenuService.ShowMenuAsync(list);
        }

        public async Task Duplicate(TaskListMVVM dusection)
        {
            if (CalcContainer.Calculation is null)
                return;

            PostStorygeDTO post = new()
            {
                Items = [new ResourceTaskItemDTO(dusection.Id, dusection.Metadata?.Quantity ?? 0)],
                Type = CalculationItemType.task,
                copyType = CopyType.Copy,
                WithCildren = true,
                ParentID = dusection.TaskId ?? 0,
                NewCalcID = CalcContainer.Calculation.Id,
                OldCalcID = CalcContainer.Calculation.Id,
                IsOH = CalcContainer.Calculation.OHFactors
            };

            bool result = await storage.CreateItem(post);
            Mhd.Notifications(ToastType.Add, result);
        }

        public void Remove(TaskListMVVM task)
        {
            if (!SelectedData.ExistItem(CalculationItemType.task, task.Id))
                Mhd.DeleteMessage(task.Name ?? string.Empty, EventCallback.Factory.Create(this, () => ConfirmedRemoveAsync([task.Id])));
            else
                Mhd.DeleteMessage(task.Name ?? string.Empty, EventCallback.Factory.Create(this, () => ConfirmedRemoveAsync([.. SelectedData.SelectedItems.Select(x => x.Id)])));
        }

        public void FromHub(OperationType ot, object obj)
        {
            var calc = CalcContainer.Calculation;
            if (calc is null)
                return;

            if (ot == OperationType.RemoveRange)
            {
                var ids = obj.FromJsonWeb<List<int>>();
                if (ids is null)
                    return;

                calc.RemoveTasks(ids);
            }
            else if (ot == OperationType.Update)
            {
                var task = obj.FromJsonWeb<TaskListMVVM>();
                if (task is null)
                    return;

                // ✅ O(1)
                if (!calc.TryGetTask(task.Id, out var oldSection) || oldSection == null)
                    return;

                // حافظ على الـ collections
                task.Tasks = oldSection.Tasks;
                task.Resources = oldSection.Resources;

                // هل يؤثر على الحساب؟
                bool affectsCalc = TaskAffectsCalculation(oldSection, task);

                task.CopyPropertiesTo(oldSection);

                // أي تغيير رقمي سيؤدي لإعادة حساب كاملة لاحقًا (batch)
                calc.LastHubChangeAffectsCalc |= affectsCalc;
            }
            else if (ot == OperationType.AddRange)
            {
                var tasks = obj.FromJsonWeb<List<TaskListMVVM>>();
                if (tasks is null)
                    return;
                calc.AddTasks(tasks);
            }
            else if (ot == OperationType.AddUpdateRange)
            {
                var tasks = obj.FromJsonWeb<List<TaskListMVVM>>();
                if (tasks is null)
                    return;

                calc.AddTasks(tasks);
            }
            else if (ot == OperationType.Add)
            {
                var hubData = obj.FromJsonWeb<HubDataDto>();
                var task = hubData?.GetData<TaskListMVVM>() ?? obj.FromJsonWeb<TaskListMVVM>();
                if (task is null)
                    return;

                calc.AddTasks([task]);
            }
            else if (ot == OperationType.MoveRange)
            {
                var list = obj.FromJsonWeb<Tuple<List<TaskListMVVM>, List<int>>>();
                if (list is null)
                    return;

                calc.RemoveTasks(list.Item2 ?? []);
                calc.AddTasks(list.Item1 ?? []);
            }
        }

        public async Task ConfirmedRemoveAsync(List<int> items)
        {
            if (CalcContainer.Calculation is null)
                return;

            var result = await Repo.DeleteAsync(CalcContainer.Calculation.Id, items);
            Mhd.Notifications(ToastType.Delete, result);
            if (result) dialogService.Close();
        }
    }
}
