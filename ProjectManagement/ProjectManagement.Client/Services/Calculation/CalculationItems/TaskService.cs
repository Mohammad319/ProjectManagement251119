using BlazorMHD.UI.Core.DesignSystem;
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
        CalculationInteractionState interactionState,
        CalculationService calculationService)
    {
        // Effective Visare permission ⇒ block grid mutations with a clear message.
        private bool DenyIfReadOnly()
        {
            if (CalcContainer.Calculation is { CanEdit: false })
            {
                Mhd.MessageOk("Behörighet", "Du har visningsbehörighet och kan inte ändra den här kalkylen.", MhdState.Warning);
                return true;
            }
            return false;
        }

        private static bool TaskAffectsCalculation(TaskListMVVM oldT, TaskListMVVM newT)
        {
            if (oldT.Quantity != newT.Quantity) return true;
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
            List<MhdContextMenuItem> list = context.BuildTaskContextMenu(task, () => Remove(task), async () => await Duplicate(task));
            await ContextMenuService.ShowMenuAsync(list);
        }

        public async Task Duplicate(TaskListMVVM dusection)
        {
            if (DenyIfReadOnly())
                return;

            var calculation = CalcContainer.Calculation;
            if (calculation is null)
                return;

            var tasks = GetSelectedOrSingleTasks(dusection);
            var groups = tasks.GroupBy(x => x.TaskId ?? 0).ToList();
            var result = true;

            foreach (var group in groups)
            {
                PostStorygeDTO post = new()
                {
                    Items = [.. group.Select(x => new ResourceTaskItemDTO(x.Id, x.Quantity ?? 0))],
                    Type = CalculationItemType.task,
                    copyType = CopyType.Copy,
                    WithCildren = true,
                    ParentID = group.Key,
                    NewCalcID = calculation.Id,
                    OldCalcID = calculation.Id,
                    IsOH = calculation.OHFactors
                };

                result &= await storage.CreateItem(post);
            }

            Mhd.Notifications(ToastType.Add, result);

            if (result)
            {
                interactionState.ResetSelection();
                await calculationService.RefreshAfterStructuralMutationAsync();
            }
        }

        public void Remove(TaskListMVVM task)
        {
            if (DenyIfReadOnly())
                return;

            if (!interactionState.IsSelected(CalculationItemType.task, task.Id))
            {
                Mhd.DeleteMessage(task.Name ?? string.Empty, EventCallback.Factory.Create(this, () => ConfirmedRemoveAsync([task.Id])));
                return;
            }

            var count = interactionState.SelectedItems.Count;
            var ids = interactionState.SelectedItems.Select(x => x.Id).ToArray();
            Mhd.MessageYesNo(
                "Delete",
                $"Are you sure you want to delete {count} items?",
                MhdState.Danger,
                EventCallback.Factory.Create(this, () => ConfirmedRemoveAsync([.. ids])));
        }

        private List<TaskListMVVM> GetSelectedOrSingleTasks(TaskListMVVM task)
        {
            var calculation = CalcContainer.Calculation;
            if (calculation is not null
                && interactionState.IsSelected(CalculationItemType.task, task.Id))
            {
                var tasks = new List<TaskListMVVM>();
                foreach (var selected in interactionState.SelectedItems)
                {
                    if (calculation.TryGetTask(selected.Id, out var selectedTask)
                        && selectedTask is not null)
                    {
                        tasks.Add(selectedTask);
                    }
                }

                if (tasks.Count > 0)
                    return tasks;
            }

            return [task];
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

                var tasks = taskDtos
                    .Select(x => x.ToTaskListMVVM())
                    .Where(task => !calc.TaskById.ContainsKey(task.Id))
                    .ToList();

                if (tasks.Count == 0)
                    return;

                calc.AddTasks(tasks);
                calc.LastHubChangeAffectsCalc = true;
            }
            else if (ot == OperationType.MoveRange)
            {
                var list = obj.FromJsonWeb<Tuple<List<TaskListDTO>, List<int>>>();
                if (list is null)
                    return;

                var movedTasks = list.Item1?.Select(x => x.ToTaskListMVVM()).ToList() ?? [];

                calc.RemoveTasks(list.Item2 ?? []);
                calc.AddTasks(movedTasks);
                calc.LastHubChangeAffectsCalc = true;
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
            await calculationService.RefreshAfterStructuralMutationAsync();
            await dialogService.CloseAsync();
        }
    }
}
