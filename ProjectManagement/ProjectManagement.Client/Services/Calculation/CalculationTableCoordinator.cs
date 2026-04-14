using BlazorMHD.UI.Core.Services;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Pages.Calculation.Form;
using ProjectManagement.Client.Pages.Calculation.Table;
using ProjectManagement.Client.Pages.Calculation.Table.DragDrop;
using ProjectManagement.Client.Pages.Project.Storage;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
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
                ? task.Resources == null || task.Resources.Count == 0
                : interactionState.ClipboardType == CalculationItemType.resource && (task.Tasks == null || task.Tasks.Count == 0);
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
                DialogSize.ExtraLarge,
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
                DialogSize.ExtraLarge,
                DialogButtonsHelper.CreateSaveCancelButtons(ResourceFormUI.DialogFormId));
        }

        public void ShowImportDialog() =>
            dialogService.ShowComponent<CSVUI>(ResourceApp.importFromFile, Icons.ImportFromFile, null, DialogSize.ExtraLarge);

        public void ShowTemplateDialog() =>
            dialogService.ShowComponent<Pages.Calculation.Template.TemplateSetDefaultUI>(
                ResourceLoc.templates,
                Icons.Template,
                new Dictionary<string, object>
                {
                    [nameof(Pages.Calculation.Template.TemplateSetDefaultUI.Tab)] = 1,
                },
                DialogSize.ExtraLarge);

        public void ShowTaskReorderDialog(TaskListMVVM? task = null) =>
            dialogService.ShowComponent<DragDropTaskUI>(
                ResourceApp.reOrder,
                Icons.ReorderRows,
                new Dictionary<string, object>
                {
                    [nameof(DragDropTaskUI.Task)] = task ?? new TaskListMVVM()
                },
                DialogSize.ExtraLarge);

        public void ShowQuantityDialog() =>
            dialogService.ShowComponent<QuantityListUI>(
                CalcResource.quantity,
                Icons.ResetQuantity,
                null,
                DialogSize.Medium,
                DialogButtonsHelper.CreateSaveCancelButtons(QuantityListUI.DialogFormId));

        public void ShowSaveToStorage(object item) =>
            dialogService.ShowComponent<SaveStorargeUI>(
                ResourceApp.save,
                Icons.SaveCloud,
                new Dictionary<string, object>
                {
                    [nameof(SaveStorargeUI.Parent)] = item
                },
                DialogSize.ExtraLarge);

        public void ShowGetFromStorage(int parentId, CalculationItemType type) =>
            dialogService.ShowComponent<GetFromStorage>(
                ResourceApp.import,
                Icons.ImportFromCloud,
                new Dictionary<string, object>
                {
                    [nameof(GetFromStorage.ParentID)] = parentId,
                    [nameof(GetFromStorage.CalcType)] = type
                },
                DialogSize.ExtraLarge);
    }
}
