using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Handless;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Services.Calculation.CalculationItems
{
    public class TaskService(ITaskRepository Repo, FolderState CalcContainer, IStorageRepository storage,
       MhdServices Mhd, IExceptionHandlers ExHandlers, ContextMenuService ContextMenuService,
        IContextMenuBuilderService context,DialogService dialogService)
    {
        public async Task ContextMenu(TaskListMVVM task)
        {
            List<MenuItem> list = context.BuildTaskContextMenu(task, () => Remove(task),
                async () => await Duplicate(task));
            await ContextMenuService.ShowMenuAsync(list);
        }

        public async Task Duplicate(TaskListMVVM dusection)
        {
            PostStorygeDTO post = new()
            {
                Items = [new ResourceTaskItemDTO(dusection.Id, dusection.Data.Quantity)],
                Type = CalculationItemType.task,
                copyType = CopyType.Copy,
                WithCildren = true,
                ParentID = dusection.TaskId ?? 0,
                NewCalcID = CalcContainer.Calculation.Id,
                OldCalcID = CalcContainer.Calculation.Id,
                IsOH = CalcContainer.Calculation.OHFactors
            };
            bool result = await ExHandlers.RunCheckTokenAsync(() => storage.CreateItem(post));
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
            if (ot == OperationType.RemoveRange)
            {
                CalcContainer.Calculation.RemoveTasks(obj.FromJsonWeb<List<int>>());
            }
            else if (ot == OperationType.Update)
            {
                var task = obj.FromJsonWeb<TaskListMVVM>();
                var oldSection = CalcContainer.Calculation.Tasks.FirstOrDefault(x => x.Id == task.Id);
                task.Tasks = oldSection.Tasks;
                task.Resources = oldSection.Resources;
                if (oldSection != null) task.CopyPropertiesTo(oldSection);
            }
            else if (ot == OperationType.AddRange)
            {
                var tasks = obj.FromJsonWeb<List<TaskListMVVM>>();
                CalcContainer.Calculation.AddTasks(tasks);
            }
            else if (ot == OperationType.MoveRange)
            {
                var list = obj.FromJsonWeb<Tuple<List<TaskListMVVM>, List<int>>>();
                if (list != null) CalcContainer.Calculation.RemoveTasks(list.Item2);
                CalcContainer.Calculation.AddTasks(list.Item1);
            }
        }
        public async Task ConfirmedRemoveAsync(List<int> items)
        {
            var result = await ExHandlers.RunCheckTokenAsync(() => Repo.DeleteAsync(CalcContainer.Calculation.Id, items));
            Mhd.Notifications(ToastType.Delete, result);
            if (result) dialogService.Close();
        }
    }
}
