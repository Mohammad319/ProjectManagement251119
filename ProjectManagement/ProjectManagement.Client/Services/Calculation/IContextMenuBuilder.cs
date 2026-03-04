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
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Services.Calculation
{
    public interface IContextMenuBuilderService
    {
        List<MenuItem> BuildGeneralContextMenu(bool isAdmin = false);
        List<MenuItem> BuildTaskContextMenu(TaskListMVVM item, Action Remove, Action Duplicate);
        List<MenuItem> BuildResourceContextMenu(ResourceListMVVM item, int taskId, Action remove, Action Duplicate);
    }

    // ملاحظة: هنا تستخدم primary constructor (C# 12 / .NET 8)
    public class ContextMenuBuilderService(
        FolderState CalcService,
        IStringLocalizer<ResourceApp> AppLoc,
        IStorageRepository Storage,
        MhdServices Mhd,
        DialogService dialogService
    ) : IContextMenuBuilderService
    {
        #region MenuItem Helpers

        // للعمليات async (ترجع Task)
        private MenuItem NewMenuItem(string icon, string label, Func<Task>? onClick) =>
            new()
            {
                Label = label,
                IconHtml = icon,
                OnClickAsync = onClick
            };

        // للعمليات العادية (Action ترجع void)
        private MenuItem NewMenuItem(string icon, string label, Action onClick) =>
            new()
            {
                Label = label,
                IconHtml = icon,
                OnClickAsync = () =>
                {
                    onClick();
                    return Task.CompletedTask; // هنا لا يوجد أي خطأ، لأن lambda ترجع Task
                }
            };

        #endregion

        public List<MenuItem> BuildGeneralContextMenu(bool isAdmin = false)
        {
            var list = new List<MenuItem>();

            // عنصر إنشاء مهمة جديدة
            MenuItem newTaskItem = NewMenuItem(
                Icons.NewTask,
                AppLoc[LocalizerConst.New, ResourceLoc.task],
                () => OpenTaskForm(new())
            );

            if (isAdmin)
            {
                list =
                [
                    newTaskItem,
                    NewMenuItem(Icons.ImportFromCloud,
                        AppLoc[LocalizerConst.Import, ResourceLoc.task],
                        () => OpenGetFromStorage(0, CalculationItemType.task)),

                    NewMenuItem(Icons.ImportFromFile,
                        ResourceApp.importFromFile,
                        () => ImportFromFile()),

                    NewMenuItem(Icons.Template,
                        ResourceLoc.template,
                        () => OpenTemplateDialog()),

                    NewMenuItem(Icons.ReorderRows,
                        ResourceApp.reOrder,
                        () => OpenReorder(null)),
                ];

                if (SelectedData.SelectedItems.Count != 0)
                    list.Add(NewMenuItem(Icons.NotSelected, ResourceLoc.unselectAll, UnSelectedAll));

                if (TemporaryData.HasPasteOption(CalculationItemType.task)
                    && (!(TemporaryData.CopyTypeo == CopyType.Move && TemporaryData.OldCalcID == CalcService.Calculation.Id)
                        || TemporaryData.CopyTypeo == CopyType.Copy))
                {
                    // هذا Async → يذهب إلى overload الأول (Func<Task>)
                    list.Add(NewMenuItem(
                        Icons.Paste,
                        ResourceApp.paste,
                        async () => await PasteItem(0)
                    ));
                }
            }

            // عناصر عامة (لا تتطلب isAdmin)
            list.Add(NewMenuItem(
                CalcService.Calculation.OnlyActive ? Icons.Active : Icons.NotActive,
                ResourceLoc.onlyActive,
                ItemsOnlyActive
            ));

            list.Add(NewMenuItem(
                CalcService.Calculation.OHFactors ? Icons.Active : Icons.NotActive,
                "OH",
                ItemsOnlyOH
            ));

            list.Add(NewMenuItem(
                Icons.ResetQuantity,
                CalcResource.quantity,
                () => dialogService.ShowComponent<QuantityListUI>(
                    CalcResource.quantity,
                    Icons.ResetQuantity,
                    null)
            ));

            return list;
        }

        void UnSelectedAll()
        {
            SelectedData.Reset();
            CalcService.Calculation.NotifyGridRefresh(flatListDirty: true);
        }

        void ItemsOnlyActive()
        {
            CalcService.Calculation.OnlyActive = !CalcService.Calculation.OnlyActive;
            CalcService.Calculation.NotifyGridRefresh(flatListDirty: true);
        }

        void ItemsOnlyOH()
        {
            CalcService.Calculation.OHFactors = !CalcService.Calculation.OHFactors;
            CalcService.Calculation.NotifyGridRefresh(flatListDirty: true);
        }

        void ImportFromFile() =>
            dialogService.ShowComponent<CSVUI>(ResourceApp.importFromFile, Icons.ImportFromFile, null, DialogSize.ExtraLarge);

        public List<MenuItem> BuildTaskContextMenu(TaskListMVVM item, Action Remove, Action Duplicate)
        {
            var list = new List<MenuItem>();

            if (item.Metadata.Type != TaskType.CodeName && (item.Tasks == null || item.Tasks.Count == 0))
            {
                list.Add(NewMenuItem(Icons.NewResource, AppLoc[LocalizerConst.New, ResourceLoc.resource], () => OpenResourceForm(new() { TaskId = item.Id })
                ));

                list.Add(NewMenuItem(
                    Icons.ImportFromCloud,
                    AppLoc[LocalizerConst.Import, ResourceLoc.resource],
                    () => OpenGetFromStorage(item.Id, CalculationItemType.resource)
                ));
            }

            if (item.Resources == null || item.Resources.Count == 0)
            {
                list.Add(NewMenuItem(
                    Icons.NewSubTask,
                    AppLoc[LocalizerConst.New, ResourceLoc.task],
                    () => OpenTaskForm(new TaskListMVVM { TaskId = item.Id })
                ));

                list.Add(NewMenuItem(
                    Icons.ImportFromCloud,
                    AppLoc[LocalizerConst.Import, ResourceLoc.SubTask],
                    () => OpenGetFromStorage(item.Id, CalculationItemType.task)
                ));
            }

            list.AddRange([
                NewMenuItem(
                    Icons.SaveCloud,
                    ResourceLoc.saveCloud,
                    () => OpenSaveToStorage(item)
                ),
                NewMenuItem(
                    Icons.Duplicate,
                    ResourceApp.duplicate,
                    Duplicate
                ),
                NewMenuItem(
                    Icons.Copy,
                    ResourceApp.copy,
                    () => TemporaryData.Copy(
                        CalcService.Calculation.Id,
                        item.Id,
                        item.Metadata.Quantity,
                        CalculationItemType.task)
                ),
            ]);

            if (TemporaryData.SelectedItems.Count > 0 &&
                ((TemporaryData.ItemsType == CalculationItemType.task && (item.Resources == null || item.Resources.Count == 0))
                || (TemporaryData.ItemsType == CalculationItemType.resource && (item.Tasks == null || item.Tasks.Count == 0))))
            {
                list.Add(NewMenuItem(
                    Icons.Paste,
                    ResourceApp.paste,
                    async () => await PasteItem(item.Id)
                ));
            }

            list.AddRange([
                NewMenuItem(
                    Icons.ReorderRows,
                    ResourceApp.reOrder,
                    () => OpenReorder(item)
                ),
                NewMenuItem(
                    Icons.Edit,
                    ResourceApp.edit,
                    () => OpenTaskForm(item)
                ),
                NewMenuItem(
                    Icons.Delete,
                    ResourceApp.delete,
                    Remove
                ),
            ]);

            return list;
        }

        public List<MenuItem> BuildResourceContextMenu(
            ResourceListMVVM item,
            int taskId,
            Action remove,
            Action Duplicate
        ) =>
            [
                NewMenuItem(
                    Icons.SaveCloud,
                    ResourceLoc.saveCloud,
                    () => OpenSaveToStorage(item)
                ),
                NewMenuItem(
                    Icons.Duplicate,
                    ResourceApp.duplicate,
                    Duplicate
                ),
                NewMenuItem(
                    Icons.Copy,
                    ResourceApp.copy,
                    () => TemporaryData.Copy(
                        CalcService.Calculation.Id,
                        item.Id,
                        item.Quantity,
                        CalculationItemType.resource)
                ),
                NewMenuItem(
                    Icons.Cut,
                    ResourceApp.cut,
                    () => TemporaryData.Cut(
                        CalcService.Calculation.Id,
                        item.Id,
                        item.Quantity,
                        CalculationItemType.resource)
                ),
                NewMenuItem(
                    Icons.Edit,
                    ResourceApp.edit,
                    () => OpenResourceForm(item)
                ),
                NewMenuItem(
                    Icons.Delete,
                    ResourceApp.delete,
                    remove
                ),
            ];

        #region Helper Methods

        private void OpenTaskForm(TaskListMVVM model)
        {
            string title = model.Id == 0
                ? AppLoc[LocalizerConst.New, ResourceLoc.task]
                : AppLoc[LocalizerConst.Update, model.Name];

            dialogService.ShowComponent<TaskFormUI>(title, Icons.NewTask,
                new Dictionary<string, object> { [nameof(TaskFormUI.Task)] = model }, DialogSize.ExtraLarge);
        }

        private void OpenResourceForm(ResourceListMVVM model)
        {
            string title = model.Id == 0
                ? AppLoc[LocalizerConst.New, ResourceLoc.resource]
                : AppLoc[LocalizerConst.Update, model.Name];

            dialogService.ShowComponent<ResourceFormUI>(title, Icons.NewResource,
                new Dictionary<string, object>
                {
                    [nameof(ResourceFormUI.Resource)] = model
                }, DialogSize.ExtraLarge);
        }

        private void OpenSaveToStorage(object obj) =>
            dialogService.ShowComponent<SaveStorargeUI>(
                ResourceApp.save,
                Icons.SaveCloud,
                new Dictionary<string, object>
                {
                    [nameof(SaveStorargeUI.Parent)] = obj
                }, DialogSize.ExtraLarge);

        private void OpenGetFromStorage(int parentID, CalculationItemType type) =>
           dialogService.ShowComponent<GetFromStorage>(
                ResourceApp.import,
                Icons.ImportFromCloud,
                new Dictionary<string, object>
                {
                    [nameof(GetFromStorage.ParentID)] = parentID,
                    [nameof(GetFromStorage.CalcType)] = type
                }, DialogSize.ExtraLarge);

        private void OpenReorder(TaskListMVVM? taskId) =>
            dialogService.ShowComponent<DragDropTaskUI>(
                ResourceApp.reOrder,
                Icons.ReorderRows,
                new Dictionary<string, object>
                {
                    [nameof(DragDropTaskUI.Task)] = taskId
                }, DialogSize.ExtraLarge);

        private void OpenTemplateDialog() =>
            dialogService.ShowComponent<Pages.Calculation.Template.TemplateSetDefaultUI>(
                ResourceLoc.templates,
                Icons.Template,
                new Dictionary<string, object>
                {
                    [nameof(Pages.Calculation.Template.TemplateSetDefaultUI.Tab)] = 1,
                }, DialogSize.ExtraLarge);

        private async Task PasteItem(int taskId)
        {
            var post = new PostStorygeDTO
            {
                copyType = TemporaryData.CopyTypeo ?? CopyType.Copy,
                OldCalcID = TemporaryData.OldCalcID,
                NewCalcID = CalcService.Calculation.Id,
                IsOH = CalcService.Calculation.OHFactors,
                ParentID = taskId,
                WithCildren = true,
                Items = TemporaryData.SelectedItems,
                Type = TemporaryData.ItemsType ?? CalculationItemType.task
            };

            bool result = await Storage.CreateItem(post);
            if (result)
            {
                SelectedData.Reset();
                Mhd.Notifications(ToastType.Info, result);
            }
            else
            {
                Mhd.Notifications(ToastType.Danger, false);
            }
        }

        #endregion
    }
}
