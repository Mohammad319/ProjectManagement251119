using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Helper.DropDown;
using ProjectManagement.Client.Pages.Folder.Component;
using ProjectManagement.Client.Pages.Project.ProjectPages;
using ProjectManagement.Client.Shared.Model.Project;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Shared.Constant;
using System.Text.Json;

namespace ProjectManagement.Client.Pages.Folder
{
    public partial class FoldersTree : IDisposable
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IClientLogger ClientLogger { get; set; } = default!;

        [Parameter] public string GroupingMode { get; set; } = ProjectTreeGroupingMode.FolderStructure;
        [Parameter] public string SortMode { get; set; } = ProjectTreeSortMode.NameAscending;

        private const string LastSelectionKey = "LastSelection";

        private sealed class LastSelection
        {
            public Guid FolderId { get; set; }
            public Guid? ProjectId { get; set; }
            public int? CalculationId { get; set; }
        }

        private bool _jsReady;
        private bool _restoreCompleted;
        private bool _restoreInProgress;

        private object? CalcDraging { get; set; }

        private sealed record ProjectDateBucket(string Label, int Year, int Quarter, bool IsUnknown);
        private sealed record GroupedProject(FolderMVVM Folder, ListProjectMVVM Project);
        private sealed record FolderProjectGroup(FolderMVVM Folder, List<ListProjectMVVM> Projects);
        private sealed record DateProjectGroup(string Label, bool IsUnknown, List<FolderProjectGroup> Folders);

        private IEnumerable<FolderMVVM> GetSortedFolders(IEnumerable<FolderMVVM>? folders)
        {
            var list = folders ?? Enumerable.Empty<FolderMVVM>();

            return SortMode switch
            {
                ProjectTreeSortMode.NameDescending => list
                    .OrderByDescending(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(folder => folder.Order),

                ProjectTreeSortMode.Status => list
                    .OrderBy(folder => folder.IsVisible ? 0 : 99)
                    .ThenBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.CreatedNewest or ProjectTreeSortMode.ModifiedNewest => list
                    .OrderByDescending(folder => folder.Order)
                    .ThenBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.CreatedOldest => list
                    .OrderBy(folder => folder.Order)
                    .ThenBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase),

                _ => list
                    .OrderBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(folder => folder.Order)
            };
        }

        private IEnumerable<ListProjectMVVM> GetSortedProjects(IEnumerable<ListProjectMVVM>? projects)
        {
            var list = projects ?? Enumerable.Empty<ListProjectMVVM>();

            return SortMode switch
            {
                ProjectTreeSortMode.NameDescending => list
                    .OrderByDescending(project => project.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(project => project.Order),

                ProjectTreeSortMode.CreatedNewest or ProjectTreeSortMode.ModifiedNewest => list
                    .OrderByDescending(project => HasDate(project.StartDate))
                    .ThenByDescending(project => project.StartDate)
                    .ThenBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.CreatedOldest => list
                    .OrderByDescending(project => HasDate(project.StartDate))
                    .ThenBy(project => project.StartDate)
                    .ThenBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.Status => list
                    .OrderBy(project => GetStatusSortRank(project.Status, project.IsVisible))
                    .ThenBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase),

                _ => list
                    .OrderBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(project => project.Order)
            };
        }

        private IEnumerable<ListCalculationMVVM> GetSortedCalculations(IEnumerable<ListCalculationMVVM>? calculations)
        {
            var list = calculations ?? Enumerable.Empty<ListCalculationMVVM>();

            return SortMode switch
            {
                ProjectTreeSortMode.NameDescending => list
                    .OrderByDescending(calculation => calculation.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(calculation => calculation.Order),

                ProjectTreeSortMode.CreatedNewest or ProjectTreeSortMode.ModifiedNewest => list
                    .OrderByDescending(calculation => HasDate(calculation.StartDate))
                    .ThenByDescending(calculation => calculation.StartDate)
                    .ThenBy(calculation => calculation.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.CreatedOldest => list
                    .OrderByDescending(calculation => HasDate(calculation.StartDate))
                    .ThenBy(calculation => calculation.StartDate)
                    .ThenBy(calculation => calculation.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.Status => list
                    .OrderBy(calculation => GetStatusSortRank(calculation.Status, true))
                    .ThenBy(calculation => calculation.Name, StringComparer.CurrentCultureIgnoreCase),

                _ => list
                    .OrderBy(calculation => calculation.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(calculation => calculation.Order)
            };
        }

        private async Task SeFolder(FolderMVVM folder)
        {
            await Folder.NewFolder(folder);
            await SaveLastSelection(folder);
        }

        private async Task SaveLastSelection(FolderMVVM folder, ListProjectMVVM? project = null, ListCalculationMVVM? calculation = null)
        {
            var selection = new LastSelection
            {
                FolderId = folder.Id,
                ProjectId = project?.Id,
                CalculationId = calculation?.Id
            };

            var json = JsonSerializer.Serialize(selection);

            try
            {
                await JS.InvokeVoidAsync("localStorage.setItem", LastSelectionKey, json);
            }
            catch (Exception ex)
            {
                await ClientLogger.ErrorAsync("Saving folder tree selection failed", ex: ex);
            }
        }

        protected override void OnInitialized()
        {
            UoWService.Folder.State.OnChange += Refresh;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                _jsReady = true;

            if (!_jsReady || _restoreCompleted || _restoreInProgress)
                return;

            if (HasActiveSelection())
            {
                _restoreCompleted = true;
                return;
            }

            if (UoWService.Folder.State.FoldersList.Count == 0)
                return;

            _restoreInProgress = true;
            try
            {
                _restoreCompleted = await RestoreLastSelectionAsync();
            }
            finally
            {
                _restoreInProgress = false;
            }
        }

        public void Dispose()
        {
            UoWService.Folder.State.OnChange -= Refresh;
        }

        public void Refresh() => InvokeAsync(StateHasChanged);

        private bool HasActiveSelection() =>
            Folder.State.FolderSelected is not null ||
            Folder.State.ProjectSelected is not null ||
            Folder.State.Calculation is not null;

        private async Task<bool> RestoreLastSelectionAsync()
        {
            string? json;

            try
            {
                json = await JS.InvokeAsync<string?>("localStorage.getItem", LastSelectionKey);
            }
            catch (Exception ex)
            {
                await ClientLogger.ErrorAsync("Loading folder tree selection failed", ex: ex);
                return true;
            }

            if (string.IsNullOrWhiteSpace(json))
                return true;

            LastSelection? selection;
            try
            {
                selection = JsonSerializer.Deserialize<LastSelection>(json);
            }
            catch (JsonException ex)
            {
                await ClearLastSelectionAsync();
                await ClientLogger.ErrorAsync("Folder tree selection payload is invalid", ex: ex);
                return true;
            }

            if (selection is null || selection.FolderId == Guid.Empty)
            {
                await ClearLastSelectionAsync();
                return true;
            }

            var folder = UoWService.Folder.State.FoldersList
                .FirstOrDefault(f => f.Id == selection.FolderId);
            if (folder is null)
            {
                await ClearLastSelectionAsync();
                return true;
            }

            await Folder.NewFolder(folder);

            if (!selection.ProjectId.HasValue)
                return true;

            var project = folder.Projects?.FirstOrDefault(p => p.Id == selection.ProjectId.Value);
            if (project is null)
            {
                await SaveLastSelection(folder);
                return true;
            }

            await Folder.SetCalcsToProject(project);
            project.ShowCalculations = true;
            Folder.State.SetCalculation(null, project, folder);

            if (!selection.CalculationId.HasValue)
                return true;

            var calculation = project.Calculations?.FirstOrDefault(c => c.Id == selection.CalculationId.Value);
            if (calculation is null)
            {
                await SaveLastSelection(folder, project);
                return true;
            }

            await CalcService.SetCalc(calculation.Id, project, folder);
            return true;
        }

        private async Task ClearLastSelectionAsync()
        {
            try
            {
                await JS.InvokeVoidAsync("localStorage.removeItem", LastSelectionKey);
            }
            catch (Exception ex)
            {
                await ClientLogger.ErrorAsync("Clearing folder tree selection failed", ex: ex);
            }
        }

        private async Task CollapseFolder(FolderMVVM folder)
        {
            await Folder.SetProjectsToFolder(folder);
            folder.ShowProjects = !folder.ShowProjects;
        }

        private async Task CollapseProject(ListProjectMVVM project)
        {
            await Folder.SetCalcsToProject(project);
            project.ShowCalculations = !project.ShowCalculations;
        }

        private async Task SetProject(FolderMVVM folder, ListProjectMVVM project)
        {
            await Folder.SetCalcsToProject(project);
            Folder.State.SetCalculation(null, project, folder);
            project.ShowCalculations = true;
            await SaveLastSelection(folder, project, null);
        }

        private async Task NewCalculations(FolderMVVM folder, ListProjectMVVM project, ListCalculationMVVM calculation)
        {
            await CalcService.SetCalc(calculation.Id, project, folder);
            await SaveLastSelection(folder, project, calculation);
        }

        private IEnumerable<DateProjectGroup> GetDateGroupedProjects()
        {
            var folders = UoWService.Folder.State.FoldersList ?? [];

            var entries = GetSortedFolders(folders)
                .Where(folder => folder.Projects is not null)
                .SelectMany(folder => GetSortedProjects(folder.Projects).Select(project => new GroupedProject(folder, project)));

            return entries
                .GroupBy(entry => GetProjectDateBucket(entry.Project))
                .OrderBy(group => group.Key.IsUnknown)
                .ThenBy(group => group.Key.Year)
                .ThenBy(group => group.Key.Quarter)
                .Select(group =>
                {
                    var unsortedFolderGroups = group
                        .GroupBy(entry => entry.Folder.Id)
                        .Select(folderGroup =>
                        {
                            var folder = folderGroup.First().Folder;
                            var projects = GetSortedProjects(folderGroup.Select(entry => entry.Project)).ToList();

                            return new FolderProjectGroup(folder, projects);
                        });

                    var folderGroups = GetSortedFolderGroups(unsortedFolderGroups).ToList();

                    return new DateProjectGroup(group.Key.Label, group.Key.IsUnknown, folderGroups);
                });
        }

        private ProjectDateBucket GetProjectDateBucket(ListProjectMVVM project)
        {
            var date = GetProjectGroupingDate(project);
            if (!date.HasValue)
                return new ProjectDateBucket("Okänt datum", int.MaxValue, int.MaxValue, true);

            if (GroupingMode == ProjectTreeGroupingMode.Year)
                return new ProjectDateBucket(date.Value.Year.ToString(), date.Value.Year, 0, false);

            var quarter = ((date.Value.Month - 1) / 3) + 1;
            return new ProjectDateBucket($"{date.Value.Year} Q{quarter}", date.Value.Year, quarter, false);
        }

        private static DateTime? GetProjectGroupingDate(ListProjectMVVM project) =>
            project.StartDate == default ? null : project.StartDate;

        private IEnumerable<FolderProjectGroup> GetSortedFolderGroups(IEnumerable<FolderProjectGroup> folderGroups)
        {
            var folderGroupById = folderGroups.ToDictionary(group => group.Folder.Id);
            return GetSortedFolders(folderGroupById.Values.Select(group => group.Folder))
                .Select(folder => folderGroupById[folder.Id]);
        }

        private static bool HasDate(DateTime date) =>
            date != default;

        private static int GetStatusSortRank(string? status, bool isVisible)
        {
            if (!isVisible)
                return 90;

            var value = (status ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(value))
                return 80;

            if (value is "active" or "pågående" or "pagaende" or "aktiv" or "ongoing")
                return 0;

            if (value.Contains("väntar") || value.Contains("vantar") || value.Contains("svar") || value.Contains("review"))
                return 10;

            if (value is "won" or "vunnen" or "completed" or "klar" or "done")
                return 20;

            if (value is "lost" or "förlorad" or "forlorad" or "cancelled" or "canceled" or "avbruten" or "avslutad")
                return 30;

            if (value is "archived" or "arkiverad")
                return 90;

            return 50;
        }

        private static string GetCalculationIndicatorClass(ListCalculationMVVM calculation)
        {
            const string baseClasses = "block h-2.5 w-2.5 rounded-full border-2 bg-white shadow-sm transition-colors dark:bg-slate-950";
            return $"{baseClasses} {GetCalculationStatusColorClass(calculation.Status)}";
        }

        private static string GetCalculationStatusColorClass(string? status)
        {
            return status?.Trim().ToLowerInvariant() switch
            {
                "draft" or "utkast" => "border-slate-400 dark:border-slate-500",
                "active" or "ongoing" or "pagaende" => "border-sky-500 dark:border-sky-400",
                "completed" or "done" or "klar" => "border-emerald-500 dark:border-emerald-400",
                "needs review" or "review" or "warning" or "varning" => "border-amber-500 dark:border-amber-400",
                "cancelled" or "canceled" or "avbruten" => "border-rose-500 dark:border-rose-400",
                "archived" or "arkiverad" => "border-slate-400 opacity-60 dark:border-slate-500",
                _ => "border-slate-400 dark:border-slate-500"
            };
        }

        private async Task HandleDrop(FolderMVVM folder)
        {
            if (CalcDraging is not FolderMVVM draggingFolder)
                return;

            draggingFolder.IsDragOver = false;
            folder.IsDragOver = false;

            double order = DropDownHelper.HandleDrop(folder, draggingFolder, UoWService.Folder.State.FoldersList);
            if (order > -1)
            {
                UoWService.Folder.State.SortFoldersDescending();
                await Repo.Folder.ReOrderAsync(draggingFolder.Id, draggingFolder.Order);
                await SaveLastSelection(draggingFolder, null, null);
            }

            CalcDraging = null;
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleDrop(ListProjectMVVM project)
        {
            if (CalcDraging is not ListProjectMVVM draggingProject)
                return;

            draggingProject.IsDragOver = false;
            project.IsDragOver = false;

            var folder = UoWService.Folder.State.FoldersList
                .FirstOrDefault(f => f.Projects != null && f.Projects.Any(p => p.Id == project.Id));
            if (folder is null)
                return;

            double order = DropDownHelper.HandleDrop(project, draggingProject, folder.Projects);
            if (order > -1)
            {
                folder.Projects = folder.Projects.OrderByDescending(x => x.Order).ToList();
                await Repo.Project.ReOrderAsync(draggingProject.Id, draggingProject.Order);
            }

            CalcDraging = null;
            await InvokeAsync(StateHasChanged);
        }

        private void OnDragEnterFolder(FolderMVVM folder)
        {
            if (CalcDraging is FolderMVVM dragging && dragging != folder)
                folder.IsDragOver = true;
        }

        private void OnDragLeaveFolder(FolderMVVM folder)
        {
            folder.IsDragOver = false;
        }

        private void ModalForm(FolderModel model) =>
            Modal.ShowComponent<FolderFormUI>(
                model.Id == Guid.Empty
                    ? AppLoc[LocalizerConst.New, ResourceLoc.folder]
                    : AppLoc[LocalizerConst.Update, model.Name],
                Icons.Folder,
                new Dictionary<string, object>
                {
                    [nameof(FolderFormUI.FolderForm)] = model,
                    [nameof(FolderFormUI.OnClickCallback)] =
                        EventCallback.Factory.Create(this, (FolderModel f) => UoWService.Folder.AddOrUpdateFolder(f))
                },
                BlazorMHD.UI.Core.Services.DialogSize.Large,
                DialogButtonsHelper.CreateSaveCancelButtons(FolderFormUI.DialogFormId)
            );

        private void ModalForm(FolderMVVM model) =>
            Modal.ShowComponent<DetailsUI>(
                model.Name,
                Icons.Details,
                new Dictionary<string, object>
                {
                    [nameof(DetailsUI.Id)] = model.Id,
                    [nameof(DetailsUI.CallBack)] = EventCallback.Factory.Create(this, Modal.Close)
                });

        private void UpdateForm(FolderMVVM folder) =>
            ModalForm(new FolderModel
            {
                Name = folder.Name,
                Color = folder.Color,
                Id = folder.Id
            });

        private async Task Context(FolderMVVM item)
        {
            List<MenuItem> list = [];

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            bool isInAnyRole = PMRolesConst.Tenant.AdminManger.Split(',').Any(r => user.IsInRole(r));

            if (!Folder.State.OtherDepartment && user.Identity?.IsAuthenticated == true && isInAnyRole)
            {
                list.Add(new()
                {
                    IconHtml = Icons.Edit,
                    Label = ResourceApp.edit,
                    OnClickAsync = () =>
                    {
                        UpdateForm(item);
                        return Task.CompletedTask;
                    }
                });

                list.Add(new()
                {
                    IconHtml = Icons.Folder,
                    Label = "Flytta mapp",
                    OnClickAsync = () =>
                    {
                        OpenMoveCopyDialog(MoveCopyItemKind.Folder, MoveCopyOperation.Move, item);
                        return Task.CompletedTask;
                    }
                });

                list.Add(new()
                {
                    IconHtml = Icons.Copy,
                    Label = "Kopiera mapp",
                    OnClickAsync = () =>
                    {
                        OpenMoveCopyDialog(MoveCopyItemKind.Folder, MoveCopyOperation.Copy, item);
                        return Task.CompletedTask;
                    }
                });

                list.Add(new()
                {
                    IconHtml = Icons.Delete,
                    Label = ResourceApp.delete,
                    OnClickAsync = () =>
                    {
                        UoWService.Folder.RemoveFolder(item);
                        return Task.CompletedTask;
                    }
                });
            }

            list.Add(new()
            {
                IconHtml = Icons.Details,
                Label = ResourceLoc.details,
                OnClickAsync = () =>
                {
                    ModalForm(item);
                    return Task.CompletedTask;
                }
            });

            await ContextService.ShowMenuAsync(list);
        }

        private void OpenMoveCopyDialog(string itemKind, string operation, FolderMVVM folder) =>
            Modal.ShowComponent<MoveCopyDialog>(
                operation == MoveCopyOperation.Copy ? "Kopiera" : "Flytta",
                new Dictionary<string, object>
                {
                    [nameof(MoveCopyDialog.ItemKind)] = itemKind,
                    [nameof(MoveCopyDialog.Operation)] = operation,
                    [nameof(MoveCopyDialog.SourceFolder)] = folder,
                    [nameof(MoveCopyDialog.OnCompleted)] = EventCallback.Factory.Create(this, RefreshAfterMoveCopyAsync)
                },
                BlazorMHD.UI.Core.Services.DialogSize.ExtraLarge);

        private async Task RefreshAfterMoveCopyAsync()
        {
            Folder.State.ClearSelection();
            await UoWService.Folder.LoadPrivateAndGroupFoldersAsync();
        }
    }
}
