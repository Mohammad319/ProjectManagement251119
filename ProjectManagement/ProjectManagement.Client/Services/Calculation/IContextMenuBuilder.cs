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
        List<MenuItem> BuildGeneralContextMenu(bool isAdmin = false);
        List<MenuItem> BuildTaskContextMenu(TaskListMVVM item, Action remove, Action duplicate);
        List<MenuItem> BuildResourceContextMenu(ResourceListMVVM item, int taskId, Action remove, Action duplicate);
    }

    public class ContextMenuBuilderService(
        FolderState folderState,
        IStringLocalizer<ResourceApp> appLoc,
        CalculationInteractionState interactionState,
        ICalculationTableCoordinator tableCoordinator) : IContextMenuBuilderService
    {
        private MenuItem NewMenuItem(string icon, string label, Func<Task>? onClick) =>
            new()
            {
                Label = label,
                IconHtml = icon,
                OnClickAsync = onClick
            };

        private MenuItem NewMenuItem(string icon, string label, Action onClick) =>
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

        public List<MenuItem> BuildGeneralContextMenu(bool isAdmin = false)
        {
            var list = new List<MenuItem>();
            var calculation = folderState.Calculation;

            MenuItem newTaskItem = NewMenuItem(
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

        public List<MenuItem> BuildTaskContextMenu(TaskListMVVM item, Action remove, Action duplicate)
        {
            var list = new List<MenuItem>();

            if (item.Metadata.Type != TaskType.CodeName && (item.Tasks == null || item.Tasks.Count == 0))
            {
                list.Add(NewMenuItem(
                    Icons.NewResource,
                    appLoc[LocalizerConst.New, ResourceLoc.resource],
                    () => tableCoordinator.ShowResourceForm(new() { TaskId = item.Id })));

                list.Add(NewMenuItem(
                    Icons.ImportFromCloud,
                    appLoc[LocalizerConst.Import, ResourceLoc.resource],
                    () => tableCoordinator.ShowGetFromStorage(item.Id, CalculationItemType.resource)));
            }

            if (item.Resources == null || item.Resources.Count == 0)
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
                        item.Metadata?.Quantity ?? 0,
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

        public List<MenuItem> BuildResourceContextMenu(
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
