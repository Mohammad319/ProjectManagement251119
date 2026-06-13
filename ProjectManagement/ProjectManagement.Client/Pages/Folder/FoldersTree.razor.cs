using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using BlazorMHD.UI.Core.Services;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Helper.DropDown;
using ProjectManagement.Client.Pages.Folder.Component;
using ProjectManagement.Client.Pages.Project.ProjectPages;
using ProjectManagement.Client.Pages.Calculation.Form;
using ProjectManagement.Client.Shared.Model.Project;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Folder;
using ProjectManagement.Shared.Helper;
using System.Text.Json;

public sealed class GroupSelectionInfo
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public string GroupType { get; init; } = "";
    public string Header { get; init; } = "";
    public List<GroupedCalcEntry> Calculations { get; init; } = [];
}

public sealed record GroupedCalcEntry(FolderMVVM Folder, ListProjectMVVM Project, ListCalculationMVVM Calculation);

namespace ProjectManagement.Client.Pages.Folder
{
    public partial class FoldersTree : IDisposable
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IClientLogger ClientLogger { get; set; } = default!;

        [Parameter] public string GroupingMode { get; set; } = ProjectTreeGroupingMode.FolderStructure;
        [Parameter] public string SortMode { get; set; } = ProjectTreeSortMode.NameAscending;
        [Parameter] public bool ShowFoldersInTree { get; set; } = true;
        [Parameter] public bool ShowProjectsInTree { get; set; } = true;
        [Parameter] public EventCallback OnExitManualOrder { get; set; }
        [Parameter] public EventCallback<GroupSelectionInfo?> OnGroupSelected { get; set; }
        [Parameter] public bool IsReorderMode { get; set; }
        [Parameter] public EventCallback OnCreateNewFolder { get; set; }

        private int _cycleStep;

        private string TreeExpandButtonTooltip => _cycleStep switch
        {
            0 => "Expandera mappar",
            1 => "Expandera projekt",
            2 => "Fäll ihop projekt",
            3 => "Fäll ihop mappar",
            _ => "Expandera mappar"
        };

        private async Task CycleExpandAsync()
        {
            switch (_cycleStep)
            {
                case 0:
                    await ExpandFoldersOnlyAsync();
                    _cycleStep = 1;
                    break;
                case 1:
                    await ExpandAllAsync();
                    _cycleStep = 2;
                    break;
                case 2:
                    CollapseProjectsOnly();
                    _cycleStep = 3;
                    break;
                case 3:
                    CollapseAll();
                    _cycleStep = 0;
                    break;
            }
            await InvokeAsync(StateHasChanged);
        }

        private async Task ContextHelaAvdelning()
        {
            var list = new List<MenuItem>();

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            bool isInAnyRole = PMRolesConst.Tenant.AdminManger.Split(',').Any(r => user.IsInRole(r));

            if (!Folder.State.OtherDepartment && user.Identity?.IsAuthenticated == true && isInAnyRole && OnCreateNewFolder.HasDelegate)
            {
                list.Add(new() { IconHtml = Icons.Folder, Label = AppLoc[LocalizerConst.New, ResourceLoc.folder], OnClickAsync = async () => await OnCreateNewFolder.InvokeAsync() });
            }

            await ContextService.ShowMenuAsync(list);
        }

        private string? _selectedGroupKey;
        private bool _previousIsReorderMode;

        private const string LastSelectionKey = "LastSelection";
        private const string LastOpenedStorageKey = "ProjectTreeLastOpened";

        private sealed class LastSelection
        {
            public Guid FolderId { get; set; }
            public Guid? ProjectId { get; set; }
            public int? CalculationId { get; set; }
        }

        private bool _jsReady;
        private bool _restoreCompleted;
        private bool _restoreInProgress;
        private bool _lastOpenedLoaded;
        private string? _previousSortMode;
        private bool _manualOrderDirty;
        private readonly Dictionary<string, int> _manualOrderSnapshot = new();
        private readonly HashSet<string> _collapsedGroupKeys = new(StringComparer.Ordinal);
        private readonly HashSet<string> _loadingNodeKeys = new(StringComparer.Ordinal);
        private Dictionary<string, long> _lastOpenedTicks = new();

        private object? CalcDraging { get; set; }

        private sealed record DateBucket(string Key, string Label, int Year, int Quarter, bool IsUnknown);
        private sealed record GroupedCalculation(FolderMVVM Folder, ListProjectMVVM Project, ListCalculationMVVM Calculation);
        private sealed record ProjectCalculationGroup(ListProjectMVVM Project, List<ListCalculationMVVM> Calculations);
        private sealed record FolderCalculationGroup(FolderMVVM Folder, List<ProjectCalculationGroup> Projects);
        private sealed record ProjectWithFolderGroup(FolderMVVM Folder, ListProjectMVVM Project, List<ListCalculationMVVM> Calculations);
        private sealed record CalculationGroupNode(string Key, string Label, string Badge, List<GroupedCalculation> Calculations, List<CalculationGroupNode>? Children = null);

        private IEnumerable<FolderMVVM> GetSortedFolders(IEnumerable<FolderMVVM>? folders)
        {
            var list = folders ?? Enumerable.Empty<FolderMVVM>();

            return SortMode switch
            {
                ProjectTreeSortMode.Manual => list
                    .OrderByDescending(folder => folder.Order)
                    .ThenBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.NameDescending => list
                    .OrderByDescending(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(folder => folder.Order),

                ProjectTreeSortMode.LastOpenedNewest => list
                    .OrderByDescending(folder => GetLastOpenedTicks(GetFolderKey(folder)))
                    .ThenBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.Status => list
                    .OrderBy(folder => folder.IsVisible ? 0 : 99)
                    .ThenBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.CreatedNewest => list
                    .OrderByDescending(folder => folder.CreatedAt)
                    .ThenBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.CreatedOldest => list
                    .OrderBy(folder => folder.CreatedAt)
                    .ThenBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.ModifiedNewest => list
                    .OrderByDescending(folder => folder.UpdatedAt ?? folder.CreatedAt)
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
                ProjectTreeSortMode.Manual => list
                    .OrderByDescending(project => project.Order)
                    .ThenBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.NameDescending => list
                    .OrderByDescending(project => project.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(project => project.Order),

                ProjectTreeSortMode.LastOpenedNewest => list
                    .OrderByDescending(project => GetLastOpenedTicks(GetProjectKey(project)))
                    .ThenBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.CreatedNewest => list
                    .OrderByDescending(project => project.CreatedAt)
                    .ThenBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.CreatedOldest => list
                    .OrderBy(project => project.CreatedAt)
                    .ThenBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.ModifiedNewest => list
                    .OrderByDescending(project => project.UpdatedAt ?? project.CreatedAt)
                    .ThenBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.Status => list
                    .OrderBy(project => project.IsArchived ? 99 : 0)
                    .ThenBy(project => project.StatusSortOrder ?? int.MaxValue)
                    .ThenBy(project => GetStatusSortRank(project.Status, !project.IsArchived))
                    .ThenBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase),

                _ => list
                    .OrderBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(project => project.Order)
            };
        }

        private IEnumerable<ListCalculationMVVM> GetSortedCalculations(IEnumerable<ListCalculationMVVM>? calculations)
        {
            var list = CalculationVersionSelector.SelectCurrentVersions(calculations);

            return SortMode switch
            {
                ProjectTreeSortMode.Manual => list
                    .OrderByDescending(calculation => calculation.Order)
                    .ThenBy(calculation => calculation.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.NameDescending => list
                    .OrderByDescending(calculation => calculation.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(calculation => calculation.Order),

                ProjectTreeSortMode.LastOpenedNewest => list
                    .OrderByDescending(calculation => GetLastOpenedTicks(GetCalculationKey(calculation)))
                    .ThenBy(calculation => calculation.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.CreatedNewest => list
                    .OrderByDescending(calculation => calculation.CreatedAt)
                    .ThenBy(calculation => calculation.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.CreatedOldest => list
                    .OrderBy(calculation => calculation.CreatedAt)
                    .ThenBy(calculation => calculation.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.ModifiedNewest => list
                    .OrderByDescending(calculation => calculation.UpdatedAt ?? calculation.CreatedAt)
                    .ThenBy(calculation => calculation.Name, StringComparer.CurrentCultureIgnoreCase),

                ProjectTreeSortMode.Status or ProjectTreeSortMode.StatusOrder => list
                    .OrderBy(calculation => calculation.StatusSortOrder ?? int.MaxValue)
                    .ThenBy(calculation => GetStatusSortRank(calculation.Status, true))
                    .ThenBy(calculation => calculation.Name, StringComparer.CurrentCultureIgnoreCase),

                _ => list
                    .OrderBy(calculation => calculation.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(calculation => calculation.Order)
            };
        }

        private async Task SeFolder(FolderMVVM folder)
        {
            _selectedGroupKey = null;
            await Folder.NewFolder(folder);
            await MarkOpenedAsync(GetFolderKey(folder));
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

        protected override void OnParametersSet()
        {
            var sortModeChanged = _previousSortMode != SortMode;
            var reorderModeChanged = _previousIsReorderMode != IsReorderMode;

            if (reorderModeChanged)
            {
                if (IsReorderMode)
                    CaptureManualOrderSnapshot();
                else if (_manualOrderDirty)
                    RestoreManualOrderSnapshot();
                _previousIsReorderMode = IsReorderMode;
            }

            if (sortModeChanged)
                _previousSortMode = SortMode;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                _jsReady = true;

            if (_jsReady && !_lastOpenedLoaded)
            {
                await LoadLastOpenedAsync();
                if (SortMode == ProjectTreeSortMode.LastOpenedNewest)
                    await InvokeAsync(StateHasChanged);
            }

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
            // Toggle first so the click responds immediately; load children async with a spinner.
            folder.ShowProjects = !folder.ShowProjects;

            if (folder.ShowProjects && !folder.ProjectsLoaded)
            {
                var key = GetFolderKey(folder);
                _loadingNodeKeys.Add(key);
                StateHasChanged();
                try
                {
                    await Folder.SetProjectsToFolder(folder);
                }
                finally
                {
                    _loadingNodeKeys.Remove(key);
                }
            }

            AddMissingManualOrderSnapshot(folder);
        }

        private async Task CollapseProject(ListProjectMVVM project)
        {
            project.ShowCalculations = !project.ShowCalculations;

            if (project.ShowCalculations && !project.CalculationsLoaded)
            {
                var key = GetProjectKey(project);
                _loadingNodeKeys.Add(key);
                StateHasChanged();
                try
                {
                    await Folder.SetCalcsToProject(project);
                }
                finally
                {
                    _loadingNodeKeys.Remove(key);
                }
            }

            AddMissingManualOrderSnapshot(project);
        }

        private async Task SetProject(FolderMVVM folder, ListProjectMVVM project)
        {
            _selectedGroupKey = null;
            await Folder.SetCalcsToProject(project);
            AddMissingManualOrderSnapshot(folder);
            AddMissingManualOrderSnapshot(project);
            Folder.State.SetCalculation(null, project, folder);
            project.ShowCalculations = true;
            await MarkOpenedAsync(GetProjectKey(project));
            await SaveLastSelection(folder, project, null);
        }

        private async Task NewCalculations(FolderMVVM folder, ListProjectMVVM project, ListCalculationMVVM calculation)
        {
            _selectedGroupKey = null;
            await CalcService.SetCalc(calculation.Id, project, folder);
            await MarkOpenedAsync(GetCalculationKey(calculation));
            await SaveLastSelection(folder, project, calculation);
        }

        private async Task SelectGroupAsync(CalculationGroupNode node, string groupType, string? parentLabel = null)
        {
            _selectedGroupKey = node.Key;

            IEnumerable<GroupedCalculation> allCalcs = node.Children?.Any() == true
                ? node.Children.SelectMany(c => c.Calculations)
                : (IEnumerable<GroupedCalculation>)node.Calculations;

            var header = groupType switch
            {
                "Status" => AppLoc["calculationsWithStatusFormat", node.Label],
                "Quarter" => AppLoc["calculationsQuarterFormat", parentLabel ?? string.Empty, node.Label],
                _ => AppLoc["calculationsYearFormat", node.Label]
            };

            // Expand each current-version entry to include all versions in its family,
            // so the group panel can support the "Visa alla versioner" toggle.
            var expandedCalcs = allCalcs
                .SelectMany(gc =>
                {
                    var family = CalculationVersionSelector
                        .GetVersions(gc.Project.Calculations, gc.Calculation);

                    return family.Select(v => new GroupedCalcEntry(gc.Folder, gc.Project, v));
                })
                .ToList();

            var info = new GroupSelectionInfo
            {
                Key = node.Key,
                Label = node.Label,
                GroupType = groupType,
                Header = header,
                Calculations = expandedCalcs
            };

            await OnGroupSelected.InvokeAsync(info);
            StateHasChanged();
        }

        private IEnumerable<(FolderMVVM Folder, ListProjectMVVM Project)> GetAllProjectsForProjectView()
        {
            var folders = UoWService.Folder.State.FoldersList ?? [];
            return GetSortedFolders(folders)
                .SelectMany(folder => GetSortedProjects(folder.Projects)
                    .Select(project => (folder, project)));
        }

        private async Task SelectWholeDepartmentAsync()
        {
            _selectedGroupKey = "hela-avdelningen";

            foreach (var folder in UoWService.Folder.State.FoldersList)
            {
                if (!folder.ProjectsLoaded)
                    await Folder.SetProjectsToFolder(folder);

                foreach (var project in folder.Projects ?? [])
                {
                    if (!project.CalculationsLoaded)
                        await Folder.SetCalcsToProject(project);
                }
            }

            var allCalcs = GetGroupedCalculations()
                .SelectMany(gc =>
                {
                    var family = CalculationVersionSelector.GetVersions(gc.Project.Calculations, gc.Calculation);
                    return family.Select(v => new GroupedCalcEntry(gc.Folder, gc.Project, v));
                })
                .ToList();

            var info = new GroupSelectionInfo
            {
                Key = "hela-avdelningen",
                Label = "Hela avdelningen",
                GroupType = "AllProjects",
                Header = "Hela avdelningen",
                Calculations = allCalcs
            };

            await OnGroupSelected.InvokeAsync(info);
            StateHasChanged();
        }

        private IEnumerable<CalculationGroupNode> GetCalculationGroupNodes()
        {
            var entries = GetGroupedCalculations().ToList();

            return GroupingMode switch
            {
                ProjectTreeGroupingMode.YearQuarter => GetYearQuarterGroups(entries),
                ProjectTreeGroupingMode.Status => GetStatusGroups(entries),
                _ => GetYearGroups(entries)
            };
        }

        private IEnumerable<GroupedCalculation> GetGroupedCalculations()
        {
            var folders = UoWService.Folder.State.FoldersList ?? [];

            return GetSortedFolders(folders)
                .SelectMany(folder => GetSortedProjects(folder.Projects)
                    .SelectMany(project => GetSortedCalculations(project.Calculations)
                        .Select(calculation => new GroupedCalculation(folder, project, calculation))));
        }

        private IEnumerable<CalculationGroupNode> GetYearGroups(IEnumerable<GroupedCalculation> entries) =>
            entries
                .GroupBy(entry => GetCalculationYearBucket(entry.Calculation))
                .OrderBy(group => group.Key.IsUnknown)
                .ThenBy(group => group.Key.Year)
                .Select(group => new CalculationGroupNode(
                    group.Key.Key,
                    group.Key.Label,
                    AppLoc["year"].Value,
                    GetSortedGroupedCalculations(group).ToList()));

        private IEnumerable<CalculationGroupNode> GetYearQuarterGroups(IEnumerable<GroupedCalculation> entries) =>
            entries
                .GroupBy(entry => GetCalculationYearBucket(entry.Calculation))
                .OrderBy(group => group.Key.IsUnknown)
                .ThenBy(group => group.Key.Year)
                .Select(yearGroup =>
                {
                    var children = yearGroup.Key.IsUnknown
                        ? null
                        : yearGroup
                            .GroupBy(entry => GetCalculationQuarterBucket(entry.Calculation))
                            .OrderBy(group => group.Key.Quarter)
                            .Select(group => new CalculationGroupNode(
                                group.Key.Key,
                                group.Key.Label,
                                AppLoc["quarter"].Value,
                                GetSortedGroupedCalculations(group).ToList()))
                            .ToList();

                    return new CalculationGroupNode(
                        yearGroup.Key.Key,
                        yearGroup.Key.Label,
                        AppLoc["year"].Value,
                        yearGroup.Key.IsUnknown ? GetSortedGroupedCalculations(yearGroup).ToList() : [],
                        children);
                });

        private IEnumerable<CalculationGroupNode> GetStatusGroups(IEnumerable<GroupedCalculation> entries) =>
            entries
                .GroupBy(entry => GetCalculationStatusBucket(entry.Calculation))
                .OrderBy(group => group.Key.IsUnknown)
                .ThenBy(group => group.Key.Year)
                .ThenBy(group => group.Key.Label, StringComparer.CurrentCultureIgnoreCase)
                .Select(group => new CalculationGroupNode(
                    group.Key.Key,
                    group.Key.Label,
                    CalcResource.status,
                    GetSortedGroupedCalculations(group).ToList()));

        private IEnumerable<ProjectWithFolderGroup> GetProjectCalculationGroupsFlat(IEnumerable<GroupedCalculation> entries)
        {
            var projectGroups = entries
                .GroupBy(entry => entry.Project.Id)
                .Select(projectGroup =>
                {
                    var first = projectGroup.First();
                    var calculations = GetSortedCalculations(projectGroup.Select(e => e.Calculation)).ToList();
                    return new ProjectWithFolderGroup(first.Folder, first.Project, calculations);
                })
                .ToList();

            var projectById = projectGroups.ToDictionary(g => g.Project.Id);
            return GetSortedProjects(projectGroups.Select(g => g.Project))
                .Select(project => projectById[project.Id]);
        }

        private IEnumerable<FolderCalculationGroup> GetFolderCalculationGroups(IEnumerable<GroupedCalculation> entries)
        {
            var folderGroups = entries
                .GroupBy(entry => entry.Folder.Id)
                .Select(folderGroup =>
                {
                    var folder = folderGroup.First().Folder;
                    var projects = folderGroup
                        .GroupBy(entry => entry.Project.Id)
                        .Select(projectGroup =>
                        {
                            var project = projectGroup.First().Project;
                            var calculations = GetSortedCalculations(projectGroup.Select(entry => entry.Calculation)).ToList();

                            return new ProjectCalculationGroup(project, calculations);
                        })
                        .ToList();

                    var projectById = projects.ToDictionary(group => group.Project.Id);
                    var sortedProjects = GetSortedProjects(projects.Select(group => group.Project))
                        .Select(project => projectById[project.Id])
                        .ToList();

                    return new FolderCalculationGroup(folder, sortedProjects);
                })
                .ToList();

            var folderById = folderGroups.ToDictionary(group => group.Folder.Id);
            return GetSortedFolders(folderGroups.Select(group => group.Folder))
                .Select(folder => folderById[folder.Id]);
        }

        private IEnumerable<GroupedCalculation> GetSortedGroupedCalculations(IEnumerable<GroupedCalculation> entries)
        {
            var byCalculationId = entries.ToDictionary(entry => entry.Calculation.Id);
            return GetSortedCalculations(byCalculationId.Values.Select(entry => entry.Calculation))
                .Select(calculation => byCalculationId[calculation.Id]);
        }

        private DateBucket GetCalculationYearBucket(ListCalculationMVVM calculation)
        {
            var date = GetCalculationGroupingDate(calculation);
            return date.HasValue
                ? new DateBucket($"year:{date.Value.Year}", date.Value.Year.ToString(), date.Value.Year, 0, false)
                : new DateBucket("year:unknown", AppLoc["unknownDate"], int.MaxValue, int.MaxValue, true);
        }

        private DateBucket GetCalculationQuarterBucket(ListCalculationMVVM calculation)
        {
            var date = GetCalculationGroupingDate(calculation);
            if (!date.HasValue)
                return new DateBucket("quarter:unknown", AppLoc["unknownDate"], int.MaxValue, int.MaxValue, true);

            var quarter = ((date.Value.Month - 1) / 3) + 1;
            return new DateBucket($"quarter:{date.Value.Year}:{quarter}", AppLoc["quarterLabel", quarter], date.Value.Year, quarter, false);
        }

        private DateBucket GetCalculationStatusBucket(ListCalculationMVVM calculation)
        {
            var status = string.IsNullOrWhiteSpace(calculation.Status)
                ? AppLoc["draft"].Value
                : calculation.Status.Trim();

            var sortOrder = calculation.StatusSortOrder ?? GetStatusSortRank(calculation.Status, true);
            return new DateBucket($"status:{status.ToLowerInvariant()}", status, sortOrder, 0, sortOrder >= 80);
        }

        private static DateTime? GetCalculationGroupingDate(ListCalculationMVVM calculation) =>
            calculation.StartDate == default ? null : calculation.StartDate;

        private bool IsNodeLoading(string key) => _loadingNodeKeys.Contains(key);

        private bool IsGroupExpanded(string key) => !_collapsedGroupKeys.Contains(key);

        private void ToggleGroup(string key)
        {
            if (!_collapsedGroupKeys.Add(key))
                _collapsedGroupKeys.Remove(key);
        }

        private static string GetGroupedFolderKey(FolderMVVM folder) => $"group-folder:{folder.Id}";
        private static string GetGroupedProjectKey(ListProjectMVVM project) => $"group-project:{project.Id}";

        private string GetGroupToggleClass(string key) =>
            $"h-3.5 w-3.5 transform transition-transform duration-200 {(IsGroupExpanded(key) ? "rotate-90" : "rotate-0")}";

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

        // Unified selection style for every node type (Hela avdelningen, folder, project, calculation).
        // Blue is the navigation/selected color; green is reserved for status meaning.
        private const string TreeSelectedRowClass =
            "border-l-2 border-sky-500 bg-sky-50 shadow-sm ring-1 ring-sky-200/70 dark:bg-sky-950/30 dark:ring-sky-800/60";

        private const string TreeRowFocusClass =
            "outline-none focus-visible:ring-2 focus-visible:ring-sky-500/50";

        // Icon chips: neutral when idle, sky when the row is selected.
        private const string TreeIconIdleClass =
            "bg-slate-100 text-slate-500 ring-1 ring-slate-200 dark:bg-slate-800 dark:text-slate-400 dark:ring-slate-700";

        private const string TreeIconSelectedClass =
            "bg-sky-100 text-sky-700 ring-1 ring-sky-200 dark:bg-sky-950/50 dark:text-sky-300 dark:ring-sky-800";

        private const string TreeIconArchivedClass =
            "bg-slate-200 text-slate-500 ring-1 ring-slate-300 dark:bg-slate-800 dark:text-slate-400 dark:ring-slate-700";

        // Discreet blue tint so projects don't look "actively selected" when idle.
        private const string TreeProjectIconIdleClass =
            "bg-slate-100 text-sky-600/80 ring-1 ring-slate-200 dark:bg-slate-800 dark:text-sky-300/70 dark:ring-slate-700";

        private const string CalculationIndicatorBaseClass =
            "block h-2.5 w-2.5 rounded-full shadow-sm ring-1 ring-inset ring-black/10 transition-colors dark:ring-white/20";

        // Same status color source as the calculation lists in the right panel.
        private static string GetCalculationStatusColor(ListCalculationMVVM calculation) =>
            CalculationStatusColor.Resolve(calculation);

        private string GetCalculationStatusTitle(ListCalculationMVVM calculation) =>
            string.IsNullOrWhiteSpace(calculation.Status) ? AppLoc["draft"].Value : calculation.Status;

        private static string GetCalculationSelectedClass(ListCalculationMVVM calculation) =>
            TreeSelectedRowClass;

        private static string GetCalculationTextClass(ListCalculationMVVM calculation, bool isSelected) =>
            isSelected
                ? "font-semibold text-sky-900 dark:text-sky-100"
                : "text-slate-600 group-hover:text-slate-900 dark:text-slate-300 dark:group-hover:text-slate-100";

        // --- Project status (alternative 3: thin colored left line) ---------------

        // Same status color source as the project list in the right panel.
        private static string GetProjectStatusColor(ListProjectMVVM project) =>
            ProjectStatusColor.Resolve(project);

        private string GetProjectStatusText(ListProjectMVVM project) =>
            project.IsArchived
                ? AppLoc["archived"].Value
                : string.IsNullOrWhiteSpace(project.Status) ? AppLoc["active"].Value : project.Status;

        private string GetProjectStatusTitle(ListProjectMVVM project) =>
            $"{AppLoc["projectStatus"]}: {GetProjectStatusText(project)}";

        // Thin vertical status bar pinned to the left edge of a project row.
        // Keeps its own status color even when the row is selected/open.
        // Stays hoverable (no pointer-events-none) so the status tooltip works.
        private const string ProjectStatusLineClass =
            "absolute left-0 top-1 bottom-1 w-1 rounded-full ring-1 ring-inset ring-black/5 dark:ring-white/10";

        // Open project: clearly active – faint blue background + marked blue frame.
        private const string ProjectRowOpenClass =
            "bg-sky-50/70 ring-1 ring-sky-200 dark:bg-sky-950/25 dark:ring-sky-800/60";

        // Closed project: resting – neutral/white background, no strong frame.
        private const string ProjectRowClosedClass =
            "bg-white dark:bg-slate-900/50";

        // --- "Show all" links for many projects / calculations -------------------

        private const int ProjectPreviewLimit = 8;
        private const int CalculationPreviewLimit = 6;

        private readonly HashSet<Guid> _expandedProjectFolders = new();
        private readonly HashSet<Guid> _expandedCalcProjects = new();

        private const string ShowMoreLinkClass =
            "ml-7 mb-0.5 inline-flex items-center gap-1 rounded-md px-1.5 py-0.5 text-[12px] font-semibold text-sky-700 transition hover:bg-sky-50 hover:text-sky-900 dark:text-sky-300 dark:hover:bg-sky-950/40 dark:hover:text-sky-100";

        // Projects are limited per folder unless the user expanded "show all"
        // (and never limited while reordering, where every row must be movable).
        private bool IsProjectsExpanded(FolderMVVM folder) =>
            IsManualOrderMode || _expandedProjectFolders.Contains(folder.Id);

        private bool IsCalcsExpanded(ListProjectMVVM project) =>
            IsManualOrderMode || _expandedCalcProjects.Contains(project.Id);

        private void ShowAllProjects(FolderMVVM folder)
        {
            _expandedProjectFolders.Add(folder.Id);
            StateHasChanged();
        }

        private void ShowAllCalculations(ListProjectMVVM project)
        {
            _expandedCalcProjects.Add(project.Id);
            StateHasChanged();
        }

        // Keep a restored/selected project visible even if it sits past the preview limit.
        private bool IsSelectedProjectHidden(List<ListProjectMVVM> sortedProjects)
        {
            var selected = Folder.State.ProjectSelected;
            if (selected is null || sortedProjects.Count <= ProjectPreviewLimit)
                return false;

            return sortedProjects.Skip(ProjectPreviewLimit).Any(p => p.Id == selected.Id);
        }

        // Keep a restored/selected calculation visible even if it sits past the preview limit.
        private bool IsSelectedCalculationHidden(List<ListCalculationMVVM> sortedCalculations)
        {
            if (Calc is null || sortedCalculations.Count <= CalculationPreviewLimit)
                return false;

            return sortedCalculations.Skip(CalculationPreviewLimit).Any(c => c.Id == Calc.Id);
        }

        private string GetFolderMeta(FolderMVVM folder) =>
            !folder.IsVisible ? AppLoc["archived"].Value : string.Empty;

        private string GetProjectMeta(ListProjectMVVM project) =>
            project.IsArchived ? AppLoc["archived"].Value : string.Empty;

        private string GetFolderTitle(FolderMVVM folder)
        {
            var count = folder.ProjectsLoaded ? $" ({folder.Projects?.Count ?? 0})" : string.Empty;
            var archived = !folder.IsVisible ? $" – {AppLoc["archived"]}" : string.Empty;
            return $"{folder.Name}{count}{archived}";
        }

        private string GetProjectTitle(ListProjectMVVM project)
        {
            var calculationCount = project.CalculationsLoaded
                ? CalculationVersionSelector.CountCurrentVersions(project.Calculations)
                : project.CalculationCount;
            var count = $" ({calculationCount})";
            var archived = project.IsArchived ? $" – {AppLoc["archived"]}" : string.Empty;
            return $"{project.Name}{count}{archived}";
        }

        private bool IsManualOrderMode => IsReorderMode;
        private bool HasManualOrderChanges => _manualOrderDirty;
        private const string ManualOrderButtonClass =
            "inline-flex h-5 w-5 items-center justify-center rounded border border-slate-200 bg-white text-[10px] font-bold leading-none text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 disabled:cursor-not-allowed disabled:opacity-30 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-slate-100";

        private static string GetFolderKey(FolderMVVM folder) => $"folder:{folder.Id}";
        private static string GetProjectKey(ListProjectMVVM project) => $"project:{project.Id}";
        private static string GetCalculationKey(ListCalculationMVVM calculation) => $"calculation:{calculation.Id}";

        private long GetLastOpenedTicks(string key) =>
            _lastOpenedTicks.TryGetValue(key, out var ticks) ? ticks : 0;

        private async Task LoadLastOpenedAsync()
        {
            try
            {
                var json = await JS.InvokeAsync<string?>("localStorage.getItem", LastOpenedStorageKey);
                if (!string.IsNullOrWhiteSpace(json))
                    _lastOpenedTicks = JsonSerializer.Deserialize<Dictionary<string, long>>(json) ?? new();
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                _lastOpenedTicks = new();
                await ClientLogger.ErrorAsync("Loading folder tree last-opened data failed", ex: ex);
            }

            _lastOpenedLoaded = true;
        }

        private async Task MarkOpenedAsync(string key)
        {
            _lastOpenedTicks[key] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (!_jsReady)
                return;

            try
            {
                await JS.InvokeVoidAsync("localStorage.setItem", LastOpenedStorageKey, JsonSerializer.Serialize(_lastOpenedTicks));
            }
            catch (Exception ex)
            {
                await ClientLogger.ErrorAsync("Saving folder tree last-opened data failed", ex: ex);
            }
        }

        private void CaptureManualOrderSnapshot()
        {
            _manualOrderSnapshot.Clear();

            foreach (var folder in UoWService.Folder.State.FoldersList)
            {
                _manualOrderSnapshot[GetFolderKey(folder)] = folder.Order;

                foreach (var project in folder.Projects ?? [])
                {
                    _manualOrderSnapshot[GetProjectKey(project)] = project.Order;

                    foreach (var calculation in project.Calculations ?? [])
                        _manualOrderSnapshot[GetCalculationKey(calculation)] = calculation.Order;
                }
            }

            _manualOrderDirty = false;
        }

        private void RestoreManualOrderSnapshot()
        {
            foreach (var folder in UoWService.Folder.State.FoldersList)
            {
                if (_manualOrderSnapshot.TryGetValue(GetFolderKey(folder), out var folderOrder))
                    folder.Order = folderOrder;

                foreach (var project in folder.Projects ?? [])
                {
                    if (_manualOrderSnapshot.TryGetValue(GetProjectKey(project), out var projectOrder))
                        project.Order = projectOrder;

                    foreach (var calculation in project.Calculations ?? [])
                    {
                        if (_manualOrderSnapshot.TryGetValue(GetCalculationKey(calculation), out var calculationOrder))
                            calculation.Order = calculationOrder;
                    }
                }
            }

            _manualOrderDirty = false;
        }

        private void AddMissingManualOrderSnapshot(FolderMVVM folder)
        {
            if (!IsManualOrderMode)
                return;

            _manualOrderSnapshot.TryAdd(GetFolderKey(folder), folder.Order);

            foreach (var project in folder.Projects ?? [])
                AddMissingManualOrderSnapshot(project);
        }

        private void AddMissingManualOrderSnapshot(ListProjectMVVM project)
        {
            if (!IsManualOrderMode)
                return;

            _manualOrderSnapshot.TryAdd(GetProjectKey(project), project.Order);

            foreach (var calculation in project.Calculations ?? [])
                _manualOrderSnapshot.TryAdd(GetCalculationKey(calculation), calculation.Order);
        }

        private void MoveFolder(FolderMVVM folder, int direction)
        {
            MoveWithinLevel(GetSortedFolders(UoWService.Folder.State.FoldersList).ToList(), folder, direction, item => item.Order, (item, order) => item.Order = order);
        }

        private void MoveProject(IEnumerable<ListProjectMVVM>? projects, ListProjectMVVM project, int direction)
        {
            MoveWithinLevel(GetSortedProjects(projects).ToList(), project, direction, item => item.Order, (item, order) => item.Order = order);
        }

        private void MoveCalculation(IEnumerable<ListCalculationMVVM>? calculations, ListCalculationMVVM calculation, int direction)
        {
            MoveWithinLevel(GetSortedCalculations(calculations).ToList(), calculation, direction, item => item.Order, (item, order) => item.Order = order);
        }

        private void MoveWithinLevel<T>(List<T> items, T item, int direction, Func<T, int> getOrder, Action<T, int> setOrder)
            where T : class
        {
            var index = items.IndexOf(item);
            var targetIndex = index + direction;

            if (index < 0 || targetIndex < 0 || targetIndex >= items.Count)
                return;

            var target = items[targetIndex];
            var currentOrder = getOrder(item);
            setOrder(item, getOrder(target));
            setOrder(target, currentOrder);
            _manualOrderDirty = true;
        }

        private bool CanMoveUp<T>(IEnumerable<T> items, T item) where T : class =>
            items.ToList().IndexOf(item) > 0;

        private bool CanMoveDown<T>(IEnumerable<T> items, T item) where T : class
        {
            var list = items.ToList();
            var index = list.IndexOf(item);
            return index >= 0 && index < list.Count - 1;
        }

        private async Task SaveManualOrderAsync()
        {
            var tasks = new List<Task<bool>>();

            foreach (var folder in UoWService.Folder.State.FoldersList)
            {
                if (HasOrderChanged(GetFolderKey(folder), folder.Order))
                    tasks.Add(Repo.Folder.ReOrderAsync(folder.Id, folder.Order));

                foreach (var project in folder.Projects ?? [])
                {
                    if (HasOrderChanged(GetProjectKey(project), project.Order))
                        tasks.Add(Repo.Project.ReOrderAsync(project.Id, project.Order));

                    foreach (var calculation in project.Calculations ?? [])
                    {
                        if (HasOrderChanged(GetCalculationKey(calculation), calculation.Order))
                            tasks.Add(Repo.Calculation.ReOrderAsync(calculation.Id, calculation.Order));
                    }
                }
            }

            var results = tasks.Count == 0
                ? Array.Empty<bool>()
                : await Task.WhenAll(tasks);

            var success = results.All(result => result);
            MHD.Notifications(ToastType.Update, success);

            if (success)
            {
                CaptureManualOrderSnapshot();
                if (OnExitManualOrder.HasDelegate)
                    await OnExitManualOrder.InvokeAsync();
            }
        }

        private async Task CancelManualOrderAsync()
        {
            RestoreManualOrderSnapshot();
            if (OnExitManualOrder.HasDelegate)
                await OnExitManualOrder.InvokeAsync();
        }

        public async Task ExpandAllAsync()
        {
            if (GroupingMode == ProjectTreeGroupingMode.FolderStructure)
            {
                foreach (var folder in UoWService.Folder.State.FoldersList)
                {
                    if (!folder.ProjectsLoaded)
                        await Folder.SetProjectsToFolder(folder);

                    if (folder.Projects?.Count > 0)
                    {
                        folder.ShowProjects = true;
                        foreach (var project in folder.Projects)
                        {
                            if (!project.CalculationsLoaded)
                                await Folder.SetCalcsToProject(project);

                            if (project.Calculations?.Count > 0)
                                project.ShowCalculations = true;
                        }
                    }
                }
            }
            else
            {
                _collapsedGroupKeys.Clear();
            }

            await InvokeAsync(StateHasChanged);
        }

        public async Task ExpandFoldersOnlyAsync()
        {
            if (GroupingMode == ProjectTreeGroupingMode.FolderStructure)
            {
                foreach (var folder in UoWService.Folder.State.FoldersList)
                {
                    if (!folder.ProjectsLoaded)
                        await Folder.SetProjectsToFolder(folder);

                    if (folder.Projects?.Count > 0)
                        folder.ShowProjects = true;
                }
            }
            else
            {
                _collapsedGroupKeys.Clear();
            }

            await InvokeAsync(StateHasChanged);
        }

        public void CollapseProjectsOnly()
        {
            if (GroupingMode == ProjectTreeGroupingMode.FolderStructure)
            {
                foreach (var folder in UoWService.Folder.State.FoldersList)
                    foreach (var project in folder.Projects ?? [])
                        project.ShowCalculations = false;
            }

            StateHasChanged();
        }

        public void CollapseAll()
        {
            if (GroupingMode == ProjectTreeGroupingMode.FolderStructure)
            {
                foreach (var folder in UoWService.Folder.State.FoldersList)
                {
                    folder.ShowProjects = false;
                    foreach (var project in folder.Projects ?? [])
                        project.ShowCalculations = false;
                }
            }
            else
            {
                foreach (var group in GetCalculationGroupNodes())
                {
                    _collapsedGroupKeys.Add(group.Key);
                    if (group.Children != null)
                        foreach (var child in group.Children)
                            _collapsedGroupKeys.Add(child.Key);
                }

                if (ShowFoldersInTree)
                    foreach (var folder in UoWService.Folder.State.FoldersList)
                        _collapsedGroupKeys.Add(GetGroupedFolderKey(folder));

                if (ShowProjectsInTree)
                    foreach (var folder in UoWService.Folder.State.FoldersList)
                        foreach (var project in folder.Projects ?? [])
                            _collapsedGroupKeys.Add(GetGroupedProjectKey(project));
            }

            StateHasChanged();
        }

        private bool HasOrderChanged(string key, int order) =>
            !_manualOrderSnapshot.TryGetValue(key, out var savedOrder) || savedOrder != order;

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
                Id = folder.Id,
                IsVisible = folder.IsVisible
            });

        private async Task Context(FolderMVVM item)
        {
            List<MenuItem> list = [];

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            bool isInAnyRole = PMRolesConst.Tenant.AdminManger.Split(',').Any(r => user.IsInRole(r));

            if (!Folder.State.OtherDepartment && user.Identity?.IsAuthenticated == true && isInAnyRole)
            {
                list.Add(new() { IconHtml = Icons.Plus, Label = AppLoc[LocalizerConst.New, CalcResource.project], OnClickAsync = () => { CreateProjectFromFolderTree(item); return Task.CompletedTask; } });
                list.Add(new() { IsSeparator = true });
                list.Add(new() { IconHtml = Icons.Edit, Label = AppLoc["editFolder"], OnClickAsync = () => { UpdateForm(item); return Task.CompletedTask; } });
                list.Add(new() { IconHtml = Icons.Folder, Label = AppLoc["moveFolder"], OnClickAsync = () => { OpenMoveCopyDialog(MoveCopyItemKind.Folder, MoveCopyOperation.Move, item); return Task.CompletedTask; } });
                list.Add(new() { IconHtml = Icons.Copy, Label = AppLoc["copyFolder"], OnClickAsync = () => { OpenMoveCopyDialog(MoveCopyItemKind.Folder, MoveCopyOperation.Copy, item); return Task.CompletedTask; } });

                if (item.IsVisible)
                    list.Add(new() { IconHtml = Icons.Archive, Label = AppLoc["archiveFolder"], OnClickAsync = async () => await ArchiveOrRestoreFolderAsync(item, archive: true) });
                else
                    list.Add(new() { IconHtml = Icons.Restore, Label = AppLoc["restoreFromArchive"], OnClickAsync = async () => await ArchiveOrRestoreFolderAsync(item, archive: false) });

                list.Add(new() { IsSeparator = true });
                list.Add(new() { IconHtml = Icons.Delete, Label = ResourceApp.delete, CssClass = "text-red-600 dark:text-red-400", OnClickAsync = () => { UoWService.Folder.RemoveFolder(item); return Task.CompletedTask; } });
            }

            await ContextService.ShowMenuAsync(list);
        }

        private async Task ContextProject(FolderMVVM folder, ListProjectMVVM project)
        {
            List<MenuItem> list = [];

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            bool isInAnyRole = PMRolesConst.Tenant.AdminManger.Split(',').Any(r => user.IsInRole(r));

            if (!Folder.State.OtherDepartment && user.Identity?.IsAuthenticated == true && isInAnyRole)
            {
                list.Add(new() { IconHtml = Icons.Plus, Label = AppLoc[LocalizerConst.New, CalcResource.calculation], OnClickAsync = async () => await CreateCalcFromProjectTreeAsync(folder, project) });
                list.Add(new() { IsSeparator = true });
                list.Add(new() { IconHtml = Icons.Edit, Label = AppLoc["editProject"], OnClickAsync = () => { EditProjectFromTree(folder, project); return Task.CompletedTask; } });
                list.Add(new() { IconHtml = Icons.Tender, Label = ResourceLoc.tender, OnClickAsync = () => { OpenProjectBidsFromTree(project); return Task.CompletedTask; } });
                list.Add(new() { IconHtml = Icons.Folder, Label = AppLoc["moveProject"], OnClickAsync = () => { OpenMoveCopyProjectDialog(folder, project, MoveCopyOperation.Move); return Task.CompletedTask; } });
                list.Add(new() { IconHtml = Icons.Copy, Label = AppLoc["copyProject"], OnClickAsync = () => { OpenMoveCopyProjectDialog(folder, project, MoveCopyOperation.Copy); return Task.CompletedTask; } });

                if (!project.IsArchived)
                    list.Add(new() { IconHtml = Icons.Archive, Label = AppLoc["archiveProject"], OnClickAsync = async () => await ArchiveOrRestoreProjectAsync(project, archive: true) });
                else
                    list.Add(new() { IconHtml = Icons.Restore, Label = AppLoc["restoreFromArchive"], OnClickAsync = async () => await ArchiveOrRestoreProjectAsync(project, archive: false) });

                list.Add(new() { IsSeparator = true });
                list.Add(new() { IconHtml = Icons.Delete, Label = ResourceApp.delete, CssClass = "text-red-600 dark:text-red-400", OnClickAsync = () => { RemoveProjectFromTree(folder, project); return Task.CompletedTask; } });
            }

            await ContextService.ShowMenuAsync(list);
        }

        private async Task ContextCalc(FolderMVVM folder, ListProjectMVVM project, ListCalculationMVVM cal)
        {
            List<MenuItem> list = [];

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            bool isInAnyRole = PMRolesConst.Tenant.AdminManger.Split(',').Any(r => user.IsInRole(r));

            list.Add(new() { IconHtml = Icons.Active, Label = AppLoc["openCalculation"], OnClickAsync = async () => await NewCalculations(folder, project, cal) });

            if (user.Identity?.IsAuthenticated == true && isInAnyRole)
            {
                list.Add(new() { IconHtml = Icons.Edit, Label = ResourceApp.edit, OnClickAsync = () => { EditCalculationFromTree(project, cal); return Task.CompletedTask; } });
                list.Add(new() { IconHtml = Icons.Folder, Label = AppLoc["moveCalculation"], OnClickAsync = () => { OpenMoveCopyCalcDialog(folder, project, cal, MoveCopyOperation.Move); return Task.CompletedTask; } });
                list.Add(new() { IconHtml = Icons.Copy, Label = AppLoc["copyCalculation"], OnClickAsync = () => { OpenMoveCopyCalcDialog(folder, project, cal, MoveCopyOperation.Copy); return Task.CompletedTask; } });
                list.Add(new() { IconHtml = Icons.Archive, Label = AppLoc["archiveCalculation"], OnClickAsync = async () => await ArchiveCalculationFromTreeAsync(project, cal) });
            }

            await ContextService.ShowMenuAsync(list);
        }

        private async Task ArchiveOrRestoreFolderAsync(FolderMVVM folder, bool archive)
        {
            if (archive)
            {
                var hasActive = (await Repo.Project.GetByFolderIdAsync(folder.Id, includeArchived: false)).Any();
                if (hasActive)
                {
                    MHD.MessageYesNo(
                        AppLoc["archiveFolderTitle"],
                        AppLoc["archiveFolderConfirm"],
                        BlazorMHD.UI.Core.DesignSystem.MhdState.Warning,
                        EventCallback.Factory.Create(this, async () => await DoFolderVisibilityUpdateAsync(folder, isVisible: false)));
                    return;
                }
            }
            await DoFolderVisibilityUpdateAsync(folder, isVisible: !archive);
        }

        private async Task DoFolderVisibilityUpdateAsync(FolderMVVM folder, bool isVisible)
        {
            var dto = new PostFolderDTO { Name = folder.Name, Color = folder.Color, IsVisible = isVisible };
            bool ok = await Repo.Folder.UpdateAsync(folder.Id, dto);
            if (ok)
            {
                folder.IsVisible = isVisible;
                UoWService.Folder.State.Notify();
            }
            MHD.Notifications(ToastType.Update, ok);
        }

        private async Task ArchiveOrRestoreProjectAsync(ListProjectMVVM project, bool archive)
        {
            var dto = await Repo.Project.GetToPostAsync(project.Id);
            if (dto is null) return;
            dto.IsArchived = archive;
            bool ok = await Repo.Project.UpdateAsync(project.Id, dto);
            if (ok)
            {
                project.IsArchived = archive;
                UoWService.Folder.State.Notify();
            }
            MHD.Notifications(ToastType.Update, ok);
        }

        private async Task ArchiveCalculationFromTreeAsync(ListProjectMVVM project, ListCalculationMVVM cal)
        {
            var dto = await Repo.Calculation.GetPostAsync(cal.Id);
            if (dto is null) return;
            dto.IsArchived = true;
            bool ok = await Repo.Calculation.UpdateAsync(dto, cal.Id);
            if (ok)
            {
                project.CalculationsLoaded = false;
                await Folder.SetCalcsToProject(project);
                UoWService.Folder.State.Notify();
            }
            MHD.Notifications(ToastType.Update, ok);
        }

        private void RemoveProjectFromTree(FolderMVVM folder, ListProjectMVVM project)
        {
            MHD.DeleteMessage(project.Name, EventCallback.Factory.Create(this, async () =>
            {
                bool ok = await Repo.Project.DeleteAsync(project.Id);
                if (ok) folder.Projects?.Remove(project);
                MHD.Notifications(ToastType.Delete, ok);
                UoWService.Folder.State.Notify();
            }));
        }

        private void RemoveCalculationFromTree(ListProjectMVVM project, ListCalculationMVVM cal)
        {
            MHD.DeleteMessage(cal.Name, EventCallback.Factory.Create(this, async () =>
            {
                bool ok = await Repo.Calculation.DeleteAsync(cal.Id);
                if (ok) project.Calculations?.Remove(cal);
                MHD.Notifications(ToastType.Delete, ok);
                UoWService.Folder.State.Notify();
            }));
        }

        private void CreateProjectFromFolderTree(FolderMVVM folder)
        {
            Modal.ShowComponent<ProjectForm>(
                AppLoc[LocalizerConst.New, CalcResource.project],
                new Dictionary<string, object>
                {
                    [nameof(ProjectForm.Project)] = new ListProjectMVVM(),
                    [nameof(ProjectForm.FolderId)] = folder.Id,
                    [nameof(ProjectForm.Callback)] = EventCallback.Factory.Create<Tuple<bool, ListProjectMVVM>>(this,
                        async t => await OnProjectEditedFromTreeAsync(folder, t))
                },
                BlazorMHD.UI.Core.Services.DialogSize.ExtraLarge,
                DialogButtonsHelper.CreateSaveCancelButtons(ProjectForm.DialogFormId));
        }

        private async Task CreateCalcFromProjectTreeAsync(FolderMVVM folder, ListProjectMVVM project)
        {
            await SetProject(folder, project);

            Modal.ShowComponent<CalculationFormUI>(
                AppLoc[LocalizerConst.New, CalcResource.calculation],
                new Dictionary<string, object>
                {
                    [nameof(CalculationFormUI.Calculation)] = new ListCalculationMVVM(),
                    [nameof(CalculationFormUI.Callback)] = EventCallback.Factory.Create<ListCalculationMVVM?>(this,
                        async updated =>
                        {
                            if (updated != null)
                            {
                                project.CalculationsLoaded = false;
                                await Folder.SetCalcsToProject(project);
                                UoWService.Folder.State.Notify();
                            }
                            Modal.Close();
                        })
                },
                BlazorMHD.UI.Core.Services.DialogSize.ExtraLarge,
                DialogButtonsHelper.CreateSaveCancelButtons(CalculationFormUI.DialogFormId));
        }

        private void EditProjectFromTree(FolderMVVM folder, ListProjectMVVM project)
        {
            Modal.ShowComponent<ProjectForm>(
                AppLoc[LocalizerConst.Update, project.Name],
                new Dictionary<string, object>
                {
                    [nameof(ProjectForm.Project)] = project,
                    [nameof(ProjectForm.FolderId)] = folder.Id,
                    [nameof(ProjectForm.Callback)] = EventCallback.Factory.Create<Tuple<bool, ListProjectMVVM>>(this,
                        async t => await OnProjectEditedFromTreeAsync(folder, t))
                },
                BlazorMHD.UI.Core.Services.DialogSize.ExtraLarge,
                DialogButtonsHelper.CreateSaveCancelButtons(ProjectForm.DialogFormId));
        }

        private async Task OnProjectEditedFromTreeAsync(FolderMVVM folder, Tuple<bool, ListProjectMVVM>? result)
        {
            if (result is null) { Modal.Close(); return; }
            folder.Projects = await Repo.Project.GetByFolderIdAsync(folder.Id, UoWService.Folder.ShowArchived);
            if (folder.Projects != null)
                folder.Projects = folder.Projects.OrderByDescending(x => x.Order).ToList();
            UoWService.Folder.State.Notify();
            Modal.Close();
        }

        private void EditCalculationFromTree(ListProjectMVVM project, ListCalculationMVVM cal)
        {
            Modal.ShowComponent<CalculationFormUI>(
                AppLoc[LocalizerConst.Update, cal.Name],
                new Dictionary<string, object>
                {
                    [nameof(CalculationFormUI.Calculation)] = cal,
                    [nameof(CalculationFormUI.Callback)] = EventCallback.Factory.Create<ListCalculationMVVM?>(this,
                        async updated =>
                        {
                            if (updated != null)
                            {
                                cal.Name = updated.Name;
                                cal.Code = updated.Code;
                                cal.Status = updated.Status;
                                UoWService.Folder.State.Notify();
                            }
                            Modal.Close();
                        })
                },
                BlazorMHD.UI.Core.Services.DialogSize.ExtraLarge,
                DialogButtonsHelper.CreateSaveCancelButtons(CalculationFormUI.DialogFormId));
        }

        private void OpenProjectBidsFromTree(ListProjectMVVM project) =>
            Modal.ShowComponent<ProjectBidsDialog>(
                ResourceLoc.tender,
                new Dictionary<string, object>
                {
                    [nameof(ProjectBidsDialog.ProjectId)] = project.Id,
                    [nameof(ProjectBidsDialog.ProjectName)] = project.Name
                },
                BlazorMHD.UI.Core.Services.DialogSize.ExtraLarge);

        private void OpenMoveCopyProjectDialog(FolderMVVM folder, ListProjectMVVM project, string operation) =>
            Modal.ShowComponent<MoveCopyDialog>(
                operation == MoveCopyOperation.Copy ? AppLoc["copyProject"] : AppLoc["moveProject"],
                new Dictionary<string, object>
                {
                    [nameof(MoveCopyDialog.ItemKind)] = MoveCopyItemKind.Project,
                    [nameof(MoveCopyDialog.Operation)] = operation,
                    [nameof(MoveCopyDialog.SourceFolder)] = folder,
                    [nameof(MoveCopyDialog.SourceProject)] = project,
                    [nameof(MoveCopyDialog.OnCompleted)] = EventCallback.Factory.Create(this, RefreshAfterMoveCopyAsync)
                },
                BlazorMHD.UI.Core.Services.DialogSize.ExtraLarge);

        private void OpenMoveCopyCalcDialog(FolderMVVM folder, ListProjectMVVM project, ListCalculationMVVM cal, string operation) =>
            Modal.ShowComponent<MoveCopyDialog>(
                operation == MoveCopyOperation.Copy ? AppLoc["copyCalculation"] : AppLoc["moveCalculation"],
                new Dictionary<string, object>
                {
                    [nameof(MoveCopyDialog.ItemKind)] = MoveCopyItemKind.Calculation,
                    [nameof(MoveCopyDialog.Operation)] = operation,
                    [nameof(MoveCopyDialog.SourceFolder)] = folder,
                    [nameof(MoveCopyDialog.SourceProject)] = project,
                    [nameof(MoveCopyDialog.SourceCalculation)] = cal,
                    [nameof(MoveCopyDialog.OnCompleted)] = EventCallback.Factory.Create(this, RefreshAfterMoveCopyAsync)
                },
                BlazorMHD.UI.Core.Services.DialogSize.ExtraLarge);

        private void OpenMoveCopyDialog(string itemKind, string operation, FolderMVVM folder) =>
            Modal.ShowComponent<MoveCopyDialog>(
                operation == MoveCopyOperation.Copy ? AppLoc["copyAction"] : AppLoc["move"],
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
