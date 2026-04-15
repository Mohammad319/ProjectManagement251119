using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.Mapping;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Base.Calculation;
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
        DialogService dialogService,
        CalculationInteractionState interactionState)
    {
        private static bool TaskAffectsCalculation(TaskListMVVM oldT, TaskListMVVM newT)
        {
            if (oldT.Metadata?.Quantity != newT.Metadata?.Quantity) return true;
            if (oldT.Metadata?.ChangeFactor1 != newT.Metadata?.ChangeFactor1) return true;
            if (GetEffectiveChangeFactor2(oldT.Metadata) != GetEffectiveChangeFactor2(newT.Metadata)) return true;
            if (oldT.Metadata?.Cap != newT.Metadata?.Cap) return true;
            return false;
        }

        private static decimal GetEffectiveChangeFactor2(TaskMetadata? metadata)
        {
            if (metadata is null)
                return 1m;

            var parameters = metadata.ConversionParameters;
            if (parameters is null || parameters.Count == 0)
                return metadata.ChangeFactor2;

            decimal product = 1m;
            for (int i = 0; i < parameters.Count; i++)
                product *= parameters[i].Value;

            return Math.Round(product, 4, MidpointRounding.AwayFromZero);
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
            if (!interactionState.IsSelected(CalculationItemType.task, task.Id))
                Mhd.DeleteMessage(task.Name ?? string.Empty, EventCallback.Factory.Create(this, () => ConfirmedRemoveAsync([task.Id])));
            else
                Mhd.DeleteMessage(task.Name ?? string.Empty, EventCallback.Factory.Create(this, () => ConfirmedRemoveAsync([.. interactionState.SelectedItems.Select(x => x.Id)])));
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
                var taskDto = obj.FromJsonWeb<TaskListDTO>();
                if (taskDto is null)
                    return;

                var task = taskDto.ToTaskListMVVM();

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
                var taskDtos = obj.FromJsonWeb<List<TaskListDTO>>();
                if (taskDtos is null)
                    return;

                var tasks = taskDtos.Select(x => x.ToTaskListMVVM()).ToList();

                calc.AddTasks(tasks);
            }
            else if (ot == OperationType.MoveRange)
            {
                var list = obj.FromJsonWeb<Tuple<List<TaskListDTO>, List<int>>>();
                if (list is null)
                    return;

                var movedTasks = list.Item1?.Select(x => x.ToTaskListMVVM()).ToList() ?? [];

                calc.RemoveTasks(list.Item2 ?? []);
                calc.AddTasks(movedTasks);
            }
        }

        public async Task ConfirmedRemoveAsync(List<int> items)
        {
            if (CalcContainer.Calculation is null)
                return;

            var result = await Repo.DeleteAsync(CalcContainer.Calculation.Id, items);
            Mhd.Notifications(ToastType.Delete, result);
            if (!result)
                return;

            interactionState.ResetSelection();
            dialogService.Close();
        }
    }
}
