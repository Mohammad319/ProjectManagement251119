using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories.Calculation;
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
            PostStorygeDTO post = new()
            {
                Items = [new ResourceTaskItemDTO(dusection.Id, dusection.Metadata.Quantity)],
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
                Mhd.DeleteMessage(task.Name, EventCallback.Factory.Create(this, () => ConfirmedRemoveAsync([task.Id])));
            else
                Mhd.DeleteMessage(task.Name, EventCallback.Factory.Create(this, () => ConfirmedRemoveAsync([.. SelectedData.SelectedItems.Select(x => x.Id)])));
        }

        public void FromHub(OperationType ot, object obj)
        {
            var calc = CalcContainer.Calculation;

            if (ot == OperationType.RemoveRange)
            {
                calc.RemoveTasks(obj.FromJsonWeb<List<int>>());
            }
            else if (ot == OperationType.Update)
            {
                var task = obj.FromJsonWeb<TaskListMVVM>();

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
                calc.AddTasks(tasks);
            }
            else if (ot == OperationType.MoveRange)
            {
                var list = obj.FromJsonWeb<Tuple<List<TaskListMVVM>, List<int>>>();
                if (list != null) calc.RemoveTasks(list.Item2);
                calc.AddTasks(list.Item1);
            }
        }

        public async Task ConfirmedRemoveAsync(List<int> items)
        {
            var result = await Repo.DeleteAsync(CalcContainer.Calculation.Id, items);
            Mhd.Notifications(ToastType.Delete, result);
            if (result) dialogService.Close();
        }
    }
}
