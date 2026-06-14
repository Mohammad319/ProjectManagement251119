using BlazorMHD.UI.Core.Services;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Services.Calculation
{
    public interface IContextMenuBuilderService
    {
        List<ContextMenuItem> BuildGeneralContextMenu(bool isAdmin = false);
        List<ContextMenuItem> BuildTaskContextMenu(TaskListMVVM item, Action remove, Action duplicate);
        List<ContextMenuItem> BuildResourceContextMenu(ResourceListMVVM item, int taskId, Action remove, Action duplicate);
    }

    public class ContextMenuBuilderService(
        FolderState folderState,
        IStringLocalizer<ResourceApp> appLoc,
        CalculationInteractionState interactionState,
        ICalculationTableCoordinator tableCoordinator) : IContextMenuBuilderService
    {
        private ContextMenuItem NewMenuItem(string icon, string label, Func<Task>? onClick) =>
            new()
            {
                Label = label,
                IconHtml = icon,
                OnClickAsync = onClick
            };

        private ContextMenuItem NewMenuItem(string icon, string label, Action onClick) =>
            new()
            {
                Label = label,
                IconHtml = icon,
                OnClickAsync = () =>
                {
                    onClick();
                    return Task.CompletedTask;
                }
            };

        public List<ContextMenuItem> BuildGeneralContextMenu(bool isAdmin = false)
        {
            var list = new List<ContextMenuItem>();
            var calculation = folderState.Calculation;

            ContextMenuItem newTaskItem = NewMenuItem(
                Icons.NewTask,
                appLoc[LocalizerConst.New, ResourceLoc.task],
                () => tableCoordinator.ShowTaskForm(new()));

            if (isAdmin)
            {
                list =
                [
                    newTaskItem,
                    NewMenuItem(Icons.ImportFromCloud,
                        appLoc[LocalizerConst.Import, ResourceLoc.task],
                        () => tableCoordinator.ShowGetFromStorage(0, CalculationItemType.task)),

                    NewMenuItem(Icons.ImportFromFile,
                        ResourceApp.importFromFile,
                        tableCoordinator.ShowImportDialog),

                    NewMenuItem(Icons.Template,
                        ResourceLoc.template,
                        tableCoordinator.ShowTemplateDialog),

                    NewMenuItem(Icons.ReorderRows,
                        ResourceApp.reOrder,
                        () => tableCoordinator.ShowTaskReorderDialog()),
                ];

                if (interactionState.SelectedItems.Count != 0)
                    list.Add(NewMenuItem(Icons.NotSelected, ResourceLoc.unselectAll, tableCoordinator.UnselectAll));

                if (tableCoordinator.CanPaste(CalculationItemType.task))
                {
                    list.Add(NewMenuItem(
                        Icons.Paste,
                        ResourceApp.paste,
                        async () => await tableCoordinator.PasteAsync(0)));
                }
            }

            list.Add(NewMenuItem(
                calculation?.OnlyActive == true ? Icons.Active : Icons.NotActive,
                ResourceLoc.onlyActive,
                tableCoordinator.ToggleOnlyActive));

            list.Add(NewMenuItem(
                calculation?.FactorDisplayMode == CalculationFactorDisplayMode.OH ? Icons.Active : Icons.NotActive,
                "OH",
                tableCoordinator.ToggleOH));

            list.Add(NewMenuItem(
                Icons.ResetQuantity,
                CalcResource.quantity,
                tableCoordinator.ShowQuantityDialog));

            return list;
        }

        public List<ContextMenuItem> BuildTaskContextMenu(TaskListMVVM item, Action remove, Action duplicate)
        {
            bool isMultiSelected = interactionState.SelectedItems.Count > 1
                && interactionState.IsSelected(CalculationItemType.task, item.Id);

            if (isMultiSelected)
                return BuildMultiTaskContextMenu(item, remove);

            var list = new List<ContextMenuItem>();

            if (TaskTypeRules.CanHaveResources(item.Metadata.Type) && (item.Tasks == null || item.Tasks.Count == 0))
            {
                list.Add(NewMenuItem(
                    Icons.NewResource,
                    appLoc[LocalizerConst.New, ResourceLoc.resource],
                    () => tableCoordinator.ShowResourceForm(new() { TaskId = item.Id })));

                list.Add(NewMenuItem(
                    Icons.ImportFromCloud,
                    appLoc[LocalizerConst.Import, ResourceLoc.resource],
                    () => tableCoordinator.ShowGetFromStorage(item.Id, CalculationItemType.resource)));

                list.Add(NewMenuItem(
                    Icons.NewResource,
                    "Suggest resources",
                    () => tableCoordinator.ShowResourceSuggestions(item)));
            }

            if (TaskTypeRules.CanHaveChildTasks(item.Metadata.Type) && (item.Resources == null || item.Resources.Count == 0))
            {
                list.Add(NewMenuItem(
                    Icons.NewSubTask,
                    appLoc[LocalizerConst.New, ResourceLoc.task],
                    () => tableCoordinator.ShowTaskForm(new TaskListMVVM { TaskId = item.Id })));

                list.Add(NewMenuItem(
                    Icons.ImportFromCloud,
                    appLoc[LocalizerConst.Import, ResourceLoc.SubTask],
                    () => tableCoordinator.ShowGetFromStorage(item.Id, CalculationItemType.task)));
            }

            list.AddRange([
                NewMenuItem(
                    Icons.SaveCloud,
                    ResourceLoc.saveCloud,
                    () => tableCoordinator.ShowSaveToStorage(item)
                ),
                NewMenuItem(
                    Icons.Duplicate,
                    ResourceApp.duplicate,
                    duplicate
                ),
                NewMenuItem(
                    Icons.Copy,
                    ResourceApp.copy,
                    () => interactionState.Copy(
                        folderState.Calculation?.Id ?? 0,
                        item.Id,
                        item.Quantity ?? 0,
                        CalculationItemType.task)
                ),
            ]);

            if (tableCoordinator.CanPasteIntoTask(item))
            {
                list.Add(NewMenuItem(
                    Icons.Paste,
                    ResourceApp.paste,
                    async () => await tableCoordinator.PasteAsync(item.Id)
                ));
            }

            list.AddRange([
                NewMenuItem(
                    Icons.ReorderRows,
                    ResourceApp.reOrder,
                    () => tableCoordinator.ShowTaskReorderDialog(item)
                ),
                NewMenuItem(
                    Icons.Edit,
                    ResourceApp.edit,
                    () => tableCoordinator.ShowTaskForm(item)
                ),
                NewMenuItem(
                    Icons.Delete,
                    ResourceApp.delete,
                    remove
                ),
            ]);

            return list;
        }

        private List<ContextMenuItem> BuildMultiTaskContextMenu(TaskListMVVM item, Action remove) =>
        [
            NewMenuItem(
                Icons.Delete,
                ResourceApp.delete,
                remove
            ),
            NewMenuItem(
                Icons.Copy,
                ResourceApp.copy,
                () => interactionState.Copy(
                    folderState.Calculation?.Id ?? 0,
                    item.Id,
                    item.Quantity ?? 0,
                    CalculationItemType.task)
            ),
            NewMenuItem(
                Icons.SaveCloud,
                ResourceLoc.saveCloud,
                () => tableCoordinator.ShowSaveToStorage(item)
            ),
            NewMenuItem(
                Icons.NewResource,
                "Suggest resources",
                () => tableCoordinator.ShowResourceSuggestions(item)
            ),
        ];

        public List<ContextMenuItem> BuildResourceContextMenu(
            ResourceListMVVM item,
            int taskId,
            Action remove,
            Action duplicate)
            =>
            [
                NewMenuItem(
                    Icons.SaveCloud,
                    ResourceLoc.saveCloud,
                    () => tableCoordinator.ShowSaveToStorage(item)
                ),
                NewMenuItem(
                    Icons.Duplicate,
                    ResourceApp.duplicate,
                    duplicate
                ),
                NewMenuItem(
                    Icons.Copy,
                    ResourceApp.copy,
                    () => interactionState.Copy(
                        folderState.Calculation?.Id ?? 0,
                        item.Id,
                        item.Quantity,
                        CalculationItemType.resource)
                ),
                NewMenuItem(
                    Icons.Cut,
                    ResourceApp.cut,
                    () => interactionState.Cut(
                        folderState.Calculation?.Id ?? 0,
                        item.Id,
                        item.Quantity,
                        CalculationItemType.resource)
                ),
                NewMenuItem(
                    Icons.Edit,
                    ResourceApp.edit,
                    () => tableCoordinator.ShowResourceForm(item)
                ),
                NewMenuItem(
                    Icons.Delete,
                    ResourceApp.delete,
                    remove
                ),
            ];
    }
}
