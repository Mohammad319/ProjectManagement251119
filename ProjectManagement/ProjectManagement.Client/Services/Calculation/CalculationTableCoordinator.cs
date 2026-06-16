using BlazorMHD.UI.Core.Services;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Pages.Calculation.Form;
using ProjectManagement.Client.Pages.Calculation.Table;
using ProjectManagement.Client.Pages.Calculation.Table.DragDrop;
using ProjectManagement.Client.Pages.Calculation.Table.ResourceSuggestions;
using ProjectManagement.Client.Pages.Project.Storage;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Services.Calculation
{
    public interface ICalculationTableCoordinator
    {
        bool CanPaste(CalculationItemType targetType);
        bool CanPasteIntoTask(TaskListMVVM task);
        void NotifyStructureRefresh();
        void ToggleOnlyActive();
        void ToggleOH();
        void UnselectAll();
        void ClearOfferSelection();
        Task PasteAsync(int taskId);
        void ShowTaskForm(TaskListMVVM model);
        void ShowResourceForm(ResourceListMVVM model);
        void ShowResourceSuggestions(TaskListMVVM task);
        void ShowResourceSuggestions(IEnumerable<TaskListMVVM> tasks);
        void ShowImportDialog();
        void ShowTemplateDialog();
        void ShowTaskReorderDialog(TaskListMVVM? task = null);
        void ShowQuantityDialog();
        void ShowSaveToStorage(object item);
        void ShowGetFromStorage(int parentId, CalculationItemType type);
    }

    public class CalculationTableCoordinator(
        FolderState folderState,
        IStorageRepository storage,
        MhdServices mhd,
        DialogService dialogService,
        CalculationInteractionState interactionState,
        CalculationService calculationService,
        IStringLocalizer<ResourceApp> appLoc) : ICalculationTableCoordinator
    {
        private CalculationMVVM? CurrentCalculation => folderState.Calculation;

        public bool CanPaste(CalculationItemType targetType)
        {
            var calculation = CurrentCalculation;
            if (calculation == null || !interactionState.CanPaste(targetType))
                return false;

            if (interactionState.ClipboardMode != CopyType.Move)
                return true;

            return interactionState.ClipboardSourceCalculationId != calculation.Id;
        }

        public bool CanPasteIntoTask(TaskListMVVM task)
        {
            if (interactionState.ClipboardItems.Count == 0 || interactionState.ClipboardType is null)
                return false;

            return interactionState.ClipboardType == CalculationItemType.task
                ? TaskTypeRules.CanHaveChildTasks(task.Type) && (task.Resources == null || task.Resources.Count == 0)
                : interactionState.ClipboardType == CalculationItemType.resource
                    && TaskTypeRules.CanHaveResources(task.Type)
                    && (task.Tasks == null || task.Tasks.Count == 0);
        }

        public void NotifyStructureRefresh() => calculationService.RequestGridRefresh(CalculationGridRefreshKind.FlatList);

        public void ToggleOnlyActive()
        {
            if (CurrentCalculation == null)
                return;

            CurrentCalculation.OnlyActive = !CurrentCalculation.OnlyActive;
            calculationService.RequestGridRefresh(CalculationGridRefreshKind.FlatList);
        }

        public void ToggleOH()
        {
            if (CurrentCalculation == null)
                return;

            CurrentCalculation.FactorDisplayMode =
                CurrentCalculation.FactorDisplayMode == CalculationFactorDisplayMode.OH
                    ? CalculationFactorDisplayMode.NetCal
                    : CalculationFactorDisplayMode.OH;

            calculationService.RequestGridRefresh(CalculationGridRefreshKind.FlatList);
        }

        public void UnselectAll()
        {
            interactionState.ResetSelection();
        }

        public void ClearOfferSelection() => calculationService.Offer = null;

        public async Task PasteAsync(int taskId)
        {
            var calculation = CurrentCalculation;
            if (calculation == null || interactionState.ClipboardMode is null || interactionState.ClipboardType is null)
                return;

            var post = new PostStorygeDTO
            {
                copyType = interactionState.ClipboardMode.Value,
                OldCalcID = interactionState.ClipboardSourceCalculationId,
                NewCalcID = calculation.Id,
                IsOH = calculation.OHFactors,
                ParentID = taskId,
                WithCildren = true,
                Items = [.. interactionState.ClipboardItems],
                Type = interactionState.ClipboardType.Value
            };

            bool result = await storage.CreateItem(post);
            if (result)
            {
                interactionState.ResetSelection();
                await calculationService.RefreshAfterStructuralMutationAsync();
                mhd.Notifications(ToastType.Info, result);
                return;
            }

            mhd.Notifications(ToastType.Danger, false);
        }

        public void ShowTaskForm(TaskListMVVM model)
        {
            string title = model.Id == 0
                ? appLoc[LocalizerConst.New, ResourceLoc.task]
                : appLoc[LocalizerConst.Update, model.Name];

            dialogService.ShowComponent<TaskFormUI>(
                title,
                Icons.NewTask,
                new Dictionary<string, object> { [nameof(TaskFormUI.Task)] = model },
                MhdDialogSize.ExtraLarge,
                DialogButtonsHelper.CreateSaveCancelButtons(TaskFormUI.DialogFormId));
        }

        public void ShowResourceForm(ResourceListMVVM model)
        {
            string title = model.Id == 0
                ? appLoc[LocalizerConst.New, ResourceLoc.resource]
                : appLoc[LocalizerConst.Update, model.Name];

            dialogService.ShowComponent<ResourceFormUI>(
                title,
                Icons.NewResource,
                new Dictionary<string, object>
                {
                    [nameof(ResourceFormUI.Resource)] = model
                },
                MhdDialogSize.ExtraLarge,
                DialogButtonsHelper.CreateSaveCancelButtons(ResourceFormUI.DialogFormId));
        }

        public void ShowResourceSuggestions(TaskListMVVM task)
        {
            if (!CanSuggestResourcesForTask(task))
                return;

            var selectedTasks = GetSelectedTasksForSuggestion(task);
            if (selectedTasks.Count > 1)
            {
                ShowResourceSuggestions(selectedTasks);
                return;
            }

            dialogService.ShowComponent<TaskResourceSuggestionsDialog>(
                $"Suggest resources ({task.Name} ({CalcResource.code}: {task.Code}) ({CalcResource.quantity}: {task.Quantity}) ({CalcResource.unit}: {task.Unit}))",
                Icons.NewResource,
                new Dictionary<string, object>
                {
                    [nameof(TaskResourceSuggestionsDialog.TaskItem)] = task 
                },
                MhdDialogSize.ExtraLarge);
        }

        public void ShowResourceSuggestions(IEnumerable<TaskListMVVM> tasks)
        {
            var calculation = CurrentCalculation;
            if (calculation is null)
                return;

            var taskList = tasks
                .Where(CanSuggestResourcesForTask)
                .DistinctBy(x => x.Id)
                .ToList();

            if (taskList.Count == 0)
                return;

            if (taskList.Count == 1)
            {
                ShowResourceSuggestions(taskList[0]);
                return;
            }

            dialogService.ShowComponent<AllTasksTopSuggestionsDialog>(
                $"Top Resource Suggestions ({taskList.Count})",
                Icons.NewResource,
                new Dictionary<string, object>
                {
                    [nameof(AllTasksTopSuggestionsDialog.CalcModel)] = calculation,
                    [nameof(AllTasksTopSuggestionsDialog.Tasks)] = taskList,
                },
                MhdDialogSize.FullScreen);
        }

        private List<TaskListMVVM> GetSelectedTasksForSuggestion(TaskListMVVM task)
        {
            var calculation = CurrentCalculation;
            if (calculation is null
                || !interactionState.IsSelected(CalculationItemType.task, task.Id))
                return [task];

            var tasks = new List<TaskListMVVM>();
            foreach (var selected in interactionState.SelectedItems)
            {
                if (calculation.TryGetTask(selected.Id, out var selectedTask)
                    && selectedTask is not null
                    && CanSuggestResourcesForTask(selectedTask))
                {
                    tasks.Add(selectedTask);
                }
            }

            return tasks.Count == 0 ? [task] : tasks;
        }

        private static bool CanSuggestResourcesForTask(TaskListMVVM task)
            => TaskTypeRules.CanHaveResources(task.Metadata.Type)
                && (task.Tasks == null || task.Tasks.Count == 0);

        public void ShowImportDialog() =>
            dialogService.ShowComponent<CSVUI>("Importera Excel-mängdförteckning", Icons.ImportFromFile, null, MhdDialogSize.FullScreen, closeOnOverlayClick: false);

        public void ShowTemplateDialog() =>
            dialogService.ShowComponent<Pages.Calculation.Template.TemplateSetDefaultUI>(
                ResourceLoc.templates,
                Icons.Template,
                new Dictionary<string, object>
                {
                    [nameof(Pages.Calculation.Template.TemplateSetDefaultUI.Tab)] = 1,
                },
                MhdDialogSize.ExtraLarge);

        public void ShowTaskReorderDialog(TaskListMVVM? task = null) =>
            dialogService.ShowComponent<DragDropTaskUI>(
                ResourceApp.reOrder,
                Icons.ReorderRows,
                new Dictionary<string, object>
                {
                    [nameof(DragDropTaskUI.Task)] = task ?? new TaskListMVVM()
                },
                MhdDialogSize.ExtraLarge);

        public void ShowQuantityDialog() =>
            dialogService.ShowComponent<QuantityListUI>(
                CalcResource.quantity,
                Icons.ResetQuantity,
                null,
                MhdDialogSize.Medium,
                DialogButtonsHelper.CreateSaveCancelButtons(QuantityListUI.DialogFormId));

        public void ShowSaveToStorage(object item) =>
            dialogService.ShowComponent<SaveStorargeUI>(
                ResourceApp.save,
                Icons.SaveCloud,
                new Dictionary<string, object>
                {
                    [nameof(SaveStorargeUI.Parent)] = item
                },
                MhdDialogSize.ExtraLarge);

        public void ShowGetFromStorage(int parentId, CalculationItemType type) =>
            dialogService.ShowComponent<GetFromStorage>(
                ResourceApp.import,
                Icons.ImportFromCloud,
                new Dictionary<string, object>
                {
                    [nameof(GetFromStorage.ParentID)] = parentId,
                    [nameof(GetFromStorage.CalcType)] = type
                },
                MhdDialogSize.ExtraLarge);
    }
}
