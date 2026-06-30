using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using BlazorMHD.UI.Core.Services;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Helper.DropDown;
using ProjectManagement.Client.Pages.Folder.Component;
using ProjectManagement.Client.Pages.Project.ProjectPages;
using ProjectManagement.Client.Pages.Calculation;
using ProjectManagement.Client.Pages.Calculation.Form;
using ProjectManagement.Client.Pages.Calculation.Share;
using ProjectManagement.Client.Shared.Model.Project;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Folder;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Helper;
using System.Security.Claims;
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
        // Server-auktoritativ "senast öppnad" per användare (samma lager som höger­panelens listor),
        // så ändringsindikatorn i trädet och i listorna är konsekvent och döljs oavsett var objektet öppnas.
        [Inject] private ProjectManagement.Client.Services.Folder.ProjectListViewPreference ProjectViewPreference { get; set; } = default!;
        [Inject] private ProjectManagement.Client.Services.Calculation.CalculationListViewPreference CalcViewPreference { get; set; } = default!;
        [Inject] private ProjectManagement.Client.Shared.Repositories.ChangeLog.IChangeLogRepository ChangeLogRepo { get; set; } = default!;

        // id (string) → senast öppnad (server). Driver ändringsindikatorn (UpdatedAt > värdet).
        private Dictionary<string, DateTime> _projectChangeSeen = new();
        private Dictionary<string, DateTime> _calcChangeSeen = new();

        // Senaste ändringar per objekt för trädets indikator-tooltip. Hämtas bara för objekt som
        // visar indikatorn (osedd ändring); signatur-vakt hindrar omladdning på varje render.
        private Dictionary<Guid, List<ProjectManagement.Shared.DTO.ChangeLog.ChangeLogItemDTO>> _projectRecentChanges = new();
        private Dictionary<int, List<ProjectManagement.Shared.DTO.ChangeLog.ChangeLogItemDTO>> _calcRecentChanges = new();
        private string _treeRecentSig = string.Empty;

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
            var list = new List<MhdContextMenuItem>();

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            bool isInAnyRole = PMRolesConst.Tenant.AdminManger.Split(',').Any(r => user.IsInRole(r));

            if (!Folder.State.OtherDepartment && !Folder.State.AllAvailable && user.Identity?.IsAuthenticated == true && isInAnyRole && OnCreateNewFolder.HasDelegate)
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

        // Selects the folder (loads its projects + shows it in the right panel)
        // without touching its expand/collapse state.
        private async Task SelectFolder(FolderMVVM folder)
        {
            _selectedGroupKey = null;
            await Folder.SetProjectsToFolder(folder);
            Folder.State.SetCalculation(null, null, folder);
            await MarkOpenedAsync(GetFolderKey(folder));
            await SaveLastSelection(folder);
        }

        // Folder-structure row click: select the folder and, for folders that have
        // children, toggle expand/collapse on the whole row (same result as the
        // chevron). Empty folders are only selected — never expanded.
        private async Task SeFolder(FolderMVVM folder)
        {
            await SelectFolder(folder);

            folder.ShowProjects = FolderHasChildren(folder) && !folder.ShowProjects;
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

        // Current user identity, read once from the auth claims so the tooltip line ("Egen avdelning" /
        // "Delad med mig · Kan visa/ändra") can be computed synchronously while rendering rows.
        private int _currentUserId;
        private int? _currentDepartmentId;

        protected override void OnInitialized()
        {
            UoWService.Folder.State.OnChange += Refresh;
        }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            _currentUserId = TryGetIntClaim(user, PMClaimsConst.UserId) ?? 0;
            _currentDepartmentId = TryGetIntClaim(user, PMClaimsConst.DepartmentId);

            // Senast öppnad per projekt/kalkyl (för ändringsindikatorn i trädet).
            _projectChangeSeen = await ProjectViewPreference.LoadLastOpenedAsync() ?? new();
            _calcChangeSeen = await CalcViewPreference.LoadLastOpenedAsync() ?? new();
        }

        // Ändringsindikator i trädet: visas efter namnet när objektet ändrats efter att aktuell
        // användare senast öppnade det. Aldrig öppnade objekt (ingen baslinje) visar ingen indikator.
        // Mappar har ingen indikator i denna första version.
        private bool HasUnseenProjectChange(ListProjectMVVM p) =>
            p.UpdatedAt is { } updated
            && (!_projectChangeSeen.TryGetValue(p.Id.ToString(), out var opened)
                ? p.ImportInfo?.IsImportedCopy == true
                : updated > opened);

        private bool HasUnseenCalcChange(ListCalculationMVVM c) =>
            c.UpdatedAt is { } updated
            && (!_calcChangeSeen.TryGetValue(c.Id.ToString(), out var opened)
                ? c.ImportInfo?.IsImportedCopy == true
                : updated > opened);

        private async Task MarkProjectChangeSeenAsync(ListProjectMVVM p)
        {
            _projectChangeSeen[p.Id.ToString()] = DateTime.UtcNow;
            await ProjectViewPreference.SaveLastOpenedAsync(_projectChangeSeen);
        }

        private async Task MarkCalcChangeSeenAsync(ListCalculationMVVM c)
        {
            _calcChangeSeen[c.Id.ToString()] = DateTime.UtcNow;
            await CalcViewPreference.SaveLastOpenedAsync(_calcChangeSeen);
        }

        private IReadOnlyList<ProjectManagement.Shared.DTO.ChangeLog.ChangeLogItemDTO>? GetProjectRecentChanges(Guid id) =>
            _projectRecentChanges.TryGetValue(id, out var list) ? list : null;

        private IReadOnlyList<ProjectManagement.Shared.DTO.ChangeLog.ChangeLogItemDTO>? GetCalcRecentChanges(int id) =>
            _calcRecentChanges.TryGetValue(id, out var list) ? list : null;

        // Förladda "Senaste ändringar" för de projekt/kalkyler i trädet som visar indikatorn (osedd
        // ändring). Signatur-vakt gör att hämtningen bara sker när uppsättningen ändras, så
        // OnAfterRender inte loopar. Mappar har ingen indikator/historik i denna version.
        private async Task EnsureTreeRecentChangesAsync()
        {
            var projectIds = new List<Guid>();
            var calcIds = new List<int>();

            foreach (var folder in UoWService.Folder.State.FoldersList ?? [])
            {
                foreach (var project in folder.Projects ?? [])
                {
                    if (HasUnseenProjectChange(project))
                        projectIds.Add(project.Id);

                    foreach (var calc in project.Calculations ?? [])
                        if (HasUnseenCalcChange(calc))
                            calcIds.Add(calc.Id);
                }
            }

            projectIds = projectIds.Distinct().OrderBy(x => x).ToList();
            calcIds = calcIds.Distinct().OrderBy(x => x).ToList();

            var projectSig = new List<string>();
            var calcSig = new List<string>();
            foreach (var folder in UoWService.Folder.State.FoldersList ?? [])
            {
                foreach (var project in folder.Projects ?? [])
                {
                    if (HasUnseenProjectChange(project))
                        projectSig.Add($"{project.Id:N}:{project.UpdatedAt?.Ticks ?? 0}");

                    foreach (var calc in project.Calculations ?? [])
                        if (HasUnseenCalcChange(calc))
                            calcSig.Add($"{calc.Id}:{calc.UpdatedAt?.Ticks ?? 0}");
                }
            }

            var sig = string.Join("|", projectSig.Distinct().OrderBy(x => x))
                + "#"
                + string.Join(",", calcSig.Distinct().OrderBy(x => x));
            if (sig == _treeRecentSig)
                return;

            _treeRecentSig = sig;
            _projectRecentChanges = projectIds.Count == 0
                ? new()
                : await ChangeLogRepo.GetRecentForProjectsAsync(projectIds);
            _calcRecentChanges = calcIds.Count == 0
                ? new()
                : await ChangeLogRepo.GetRecentForCalculationsAsync(calcIds);

            await InvokeAsync(StateHasChanged);
        }

        private static int? TryGetIntClaim(ClaimsPrincipal user, string claimType)
            => int.TryParse(user.FindFirst(claimType)?.Value, out var value) && value > 0 ? value : null;

        // Folder management is blocked when the folder is a read-only shared/visual group, or the whole
        // department is "another department" (single 👥 department selected). Per-folder so the
        // "Alla tillgängliga" tree can mix editable (own department) and read-only (shared) folders.
        private bool IsFolderManageBlocked(FolderMVVM folder) =>
            folder.IsReadOnlyGroup || Folder.State.OtherDepartment;

        // Tree root row label: "Alla tillgängliga" for the special scope, otherwise "Hela avdelningen".
        private string TreeRootLabel => Folder.State.AllAvailable ? "Alla tillgängliga" : "Hela avdelningen";

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

            // Förladda trädets indikator-tooltipar (signatur-vakt internt → ingen render-loop).
            await EnsureTreeRecentChangesAsync();

            if (!_jsReady || _restoreCompleted || _restoreInProgress)
                return;

            // A notification deep-link ("Open project/calculation") is picking the target explicitly.
            // Skip restoring the last localStorage selection so it can't override that choice; once the
            // deep-link applies its selection, HasActiveSelection() below ends the restore for this load.
            if (Folder.State.SuppressLastSelectionRestore)
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
            // Only expand a restored project if it has calculations to show.
            project.ShowCalculations = ProjectHasChildren(project);
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

        // Selects the project (loads its calculations + shows it in the right panel)
        // without touching its expand/collapse state.
        private async Task SelectProject(FolderMVVM folder, ListProjectMVVM project)
        {
            _selectedGroupKey = null;
            await Folder.SetCalcsToProject(project);
            AddMissingManualOrderSnapshot(folder);
            AddMissingManualOrderSnapshot(project);
            Folder.State.SetCalculation(null, project, folder);
            await MarkOpenedAsync(GetProjectKey(project));
            await MarkProjectChangeSeenAsync(project);
            await SaveLastSelection(folder, project, null);
        }

        // Folder-structure row click: select the project and, for projects that have
        // calculations, toggle expand/collapse on the whole row (same result as the
        // chevron). Empty projects are only selected — never expanded.
        private async Task SetProject(FolderMVVM folder, ListProjectMVVM project)
        {
            await SelectProject(folder, project);

            project.ShowCalculations = ProjectHasChildren(project) && !project.ShowCalculations;
        }

        private async Task NewCalculations(FolderMVVM folder, ListProjectMVVM project, ListCalculationMVVM calculation)
        {
            _selectedGroupKey = null;
            await CalcService.SetCalc(calculation.Id, project, folder);
            await MarkOpenedAsync(GetCalculationKey(calculation));
            await MarkCalcChangeSeenAsync(calculation);
            await SaveLastSelection(folder, project, calculation);
        }

        private async Task SelectGroupAsync(CalculationGroupNode node, string groupType, string? parentLabel = null)
        {
            _selectedGroupKey = node.Key;

            IEnumerable<GroupedCalculation> allCalcs = node.Children?.Count > 0
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
                Label = TreeRootLabel,
                GroupType = "AllProjects",
                Header = TreeRootLabel,
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

        // A node only gets an expand chevron / expanded-state when it actually has
        // children. Before the children are lazily loaded we rely on the count that
        // came with the list; afterwards we trust the loaded collection.
        private static bool FolderHasChildren(FolderMVVM folder) =>
            folder.ProjectsLoaded
                ? (folder.Projects?.Count ?? 0) > 0
                : folder.ProjectCount > 0;

        private static bool ProjectHasChildren(ListProjectMVVM project) =>
            project.CalculationsLoaded
                ? (project.Calculations?.Count ?? 0) > 0
                : project.CalculationCount > 0;

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
        // Selected row: tydligare men fortfarande blå (aldrig orange/tung grå). En lite kraftigare
        // ljusblå bakgrund + en tydligare 3px blå vänsterkant, så valt objekt syns klart på alla
        // nivåer även i långa träd – utan ring/skugga som skulle skrika över huvudtabellen.
        private const string TreeSelectedRowClass =
            "border border-sky-200 border-l-4 border-l-sky-500 bg-sky-100 text-sky-950 shadow-sm dark:border-sky-900/70 dark:border-l-sky-400 dark:bg-sky-950/60 dark:text-sky-100";

        // Expanded (but not selected): a faint tint, no ring.
        private const string TreeExpandedRowClass =
            "bg-slate-50 hover:bg-slate-100 dark:bg-slate-900/50 dark:hover:bg-slate-800/70";

        private const string TreeNormalRowClass =
            "bg-white hover:bg-slate-50 dark:bg-slate-900/50 dark:hover:bg-slate-800/60";

        private const string TreeRowFocusClass =
            "outline-none focus-visible:ring-2 focus-visible:ring-sky-500/50";

        // Icon chips: discreet (no ring, soft background) when idle, soft sky when selected.
        private const string TreeIconIdleClass =
            "bg-slate-100/70 text-slate-400 dark:bg-slate-800/60 dark:text-slate-500";

        private const string TreeIconSelectedClass =
            "bg-sky-200 text-sky-800 ring-1 ring-inset ring-sky-300 dark:bg-sky-900/70 dark:text-sky-200 dark:ring-sky-700";

        private const string TreeIconArchivedClass =
            "bg-slate-100/70 text-slate-400 dark:bg-slate-800/60 dark:text-slate-500";

        // Discreet blue tint so projects don't look "actively selected" when idle.
        private const string TreeProjectIconIdleClass =
            "bg-slate-100/70 text-sky-600/70 dark:bg-slate-800/60 dark:text-sky-300/70";

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

        private static int GetFolderProjectCount(FolderMVVM folder) =>
            folder.ProjectsLoaded ? folder.Projects?.Count ?? 0 : folder.ProjectCount;

        private static string GetFolderProjectCountLabel(FolderMVVM folder)
        {
            var count = GetFolderProjectCount(folder);
            return count == 1 ? "1 projekt" : $"{count} projekt";
        }

        private static string GetFolderTitle(FolderMVVM folder)
        {
            var line1 = $"{folder.Name} · {GetFolderProjectCountLabel(folder)}";
            // Read-only shared folder: explain it is only a visual grouping of shared/assigned projects.
            return folder.IsReadOnlyGroup
                ? $"{line1}\nDelade med mig · visuell grupp, mappen kan inte ändras"
                : line1;
        }

        private static int GetProjectCalculationCount(ListProjectMVVM project) =>
            project.CalculationsLoaded
                ? CalculationVersionSelector.CountCurrentVersions(project.Calculations)
                : project.CalculationCount;

        private static bool ShouldShowProjectCalculationCount(ListProjectMVVM project) =>
            project.CalculationsLoaded || project.CalculationCount > 0;

        private static string GetProjectCalculationCountLabel(ListProjectMVVM project)
        {
            var count = GetProjectCalculationCount(project);
            return count == 1 ? "1 kalkyl" : $"{count} kalkyler";
        }

        private string GetProjectTitle(ListProjectMVVM project)
        {
            var parts = new[] { project.Code, project.Name, GetProjectCalculationCountLabel(project) }
                .Where(x => !string.IsNullOrWhiteSpace(x));
            var line1 = string.Join(" · ", parts);
            return $"{line1}\n{GetProjectAccessLine(project)}";
        }

        // Second tooltip line: explains how the user reaches the project.
        // "Egen avdelning" for normal department access, otherwise
        // "Delad med mig · Kan visa" / "Delad med mig · Kan ändra" based on the share role.
        private string GetProjectAccessLine(ListProjectMVVM project)
        {
            var access = project.Access;
            if (access is null || access.ViaDepartment)
                return "Egen avdelning";

            var role = GetMatchingShareRole(access);
            return role is not null
                ? $"Delad med mig · {AccessSummaryFormatter.AccessLevelLabel(role)}"
                : "Delad med mig";
        }

        // Role of the share that grants the current user access (direct user share or via their department).
        private string? GetMatchingShareRole(ProjectAccessSummaryDTO access) =>
            access.Recipients.FirstOrDefault(r =>
                (r.UserId.HasValue && r.UserId.Value == _currentUserId) ||
                (r.DepartmentId.HasValue && _currentDepartmentId.HasValue && r.DepartmentId.Value == _currentDepartmentId.Value))
            ?.Role;

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

        // I ordningsläge visas upp/ner-knapparna alltid (konsekvent), men inaktiveras när flytt inte är
        // tillåten. Dessa metoder returnerar tooltip-förklaringen (null = flytt tillåten). Sorteringen är
        // alltid manuell i ordningsläge, så den orsaken är inte aktuell här.
        private const string FolderMoveBlockedReason =
            "Mappar i delade avdelningar är bara visuella grupper och kan inte flyttas.";
        private const string ItemMoveBlockedReason =
            "Du kan inte ändra ordning i en delad vy.";

        private string? MoveUpReason<T>(IEnumerable<T> items, T item, bool blocked, string blockedReason) where T : class =>
            blocked ? blockedReason : (CanMoveUp(items, item) ? null : "Objektet ligger redan först.");

        private string? MoveDownReason<T>(IEnumerable<T> items, T item, bool blocked, string blockedReason) where T : class =>
            blocked ? blockedReason : (CanMoveDown(items, item) ? null : "Objektet ligger redan sist.");

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
                BlazorMHD.UI.Core.Services.MhdDialogSize.Large,
                DialogButtonsHelper.CreateSaveCancelButtons(FolderFormUI.DialogFormId)
            );

        private void ModalForm(FolderMVVM model) =>
            Modal.ShowComponent<DetailsUI>(
                model.Name,
                Icons.Details,
                new Dictionary<string, object>
                {
                    [nameof(DetailsUI.Id)] = model.Id,
                    [nameof(DetailsUI.CallBack)] = EventCallback.Factory.Create(this, Modal.CloseAsync)
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
            List<MhdContextMenuItem> list = [];

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            bool isInAnyRole = PMRolesConst.Tenant.AdminManger.Split(',').Any(r => user.IsInRole(r));
            bool canManageFolder = !IsFolderManageBlocked(item) && user.Identity?.IsAuthenticated == true && isInAnyRole;

            // Mapphantering kräver normal avdelningsåtkomst eller adminbehörighet. Extra
            // projektdelning ger INTE rätt att hantera mappar, så menyn döljs helt för
            // användare utan behörighet (samma grupperade ordning/utseende som projekt-
            // och kalkylmenyerna, separerade med avdelare).
            if (canManageFolder)
            {
                // Grupp 1 — skapa/importera
                list.Add(new() { IconHtml = Icons.Plus, Label = AppLoc["newProject"], OnClickAsync = () => { CreateProjectFromFolderTree(item); return Task.CompletedTask; } });
                // Import a project copy (.atacost) into this folder — creates a new project.
                list.Add(new() { IconHtml = Icons.ImportFromFile, Label = "Importera projekt...", OnClickAsync = () => RequestImportProjectCopy(item, canManageFolder) });

                // Grupp 2 — ändra mapp
                list.Add(new() { IsSeparator = true });
                list.Add(new() { IconHtml = Icons.Edit, Label = AppLoc["editFolder"], OnClickAsync = () => { UpdateForm(item); return Task.CompletedTask; } });

                // Grupp 3 — mapphantering
                list.Add(new() { IsSeparator = true });
                list.Add(new() { IconHtml = Icons.Folder, Label = AppLoc["moveFolder"], OnClickAsync = () => { OpenMoveCopyDialog(MoveCopyItemKind.Folder, MoveCopyOperation.Move, item); return Task.CompletedTask; } });
                list.Add(new() { IconHtml = Icons.Copy, Label = AppLoc["copyFolder"], OnClickAsync = () => { OpenMoveCopyDialog(MoveCopyItemKind.Folder, MoveCopyOperation.Copy, item); return Task.CompletedTask; } });

                if (item.IsVisible)
                    list.Add(new() { IconHtml = Icons.Archive, Label = AppLoc["archiveFolder"], OnClickAsync = async () => await ArchiveOrRestoreFolderAsync(item, archive: true) });
                else
                    list.Add(new() { IconHtml = Icons.Restore, Label = AppLoc["restoreFromArchive"], OnClickAsync = async () => await ArchiveOrRestoreFolderAsync(item, archive: false) });

                // Grupp 4 — destruktiv åtgärd: "Ta bort mapp" ligger alltid sist, röd, med separator före.
                // Aktiv för helt tomma mappar; annars öppnas en informationsdialog som förklarar
                // varför borttagning inte är tillåten (innehåll eller behörighet).
                list.Add(new() { IsSeparator = true });
                list.Add(new()
                {
                    IconHtml = Icons.Delete,
                    Label = "Ta bort mapp",
                    CssClass = "text-red-600 dark:text-red-400",
                    OnClickAsync = () => RequestDeleteFolderAsync(item, canManageFolder)
                });
            }

            if (list.Count == 0)
                return;

            await ContextService.ShowMenuAsync(list);
        }

        // ---- Ta bort mapp: enkel och säker regelhantering -----------------------
        // En mapp får bara tas bort om den är helt tom. Innehåller den projekt eller
        // arkiverade projekt visas en informationsdialog som förklarar varför
        // borttagning inte är tillåten. "Arkivera mapp" finns kvar som säkert
        // alternativ för mappar med innehåll/historik.
        private async Task RequestDeleteFolderAsync(FolderMVVM folder, bool canManageFolder)
        {
            const string blockedTitle = "Mappen kan inte tas bort";

            // Behörighet
            if (!canManageFolder)
            {
                MHD.MessageOk(blockedTitle, "Du saknar behörighet att ta bort mappar.",
                    BlazorMHD.UI.Core.DesignSystem.MhdState.Warning);
                return;
            }

            // Hämta mappens innehåll – inklusive arkiverade projekt – så att en mapp
            // inte kan tas bort bara för att projekten är dolda i aktuell vy.
            List<ListProjectMVVM> projects;
            try
            {
                projects = await Repo.Project.GetByFolderIdAsync(folder.Id, includeArchived: true) ?? [];
            }
            catch (Exception ex)
            {
                await ClientLogger.ErrorAsync("Checking folder contents before delete failed", ex: ex);
                MHD.MessageOk(blockedTitle,
                    "Det gick inte att kontrollera mappens innehåll. Försök igen.",
                    BlazorMHD.UI.Core.DesignSystem.MhdState.Danger);
                return;
            }

            // Mapp med projekt
            if (projects.Any(p => !p.IsArchived))
            {
                MHD.MessageOk(blockedTitle,
                    "Mappen kan inte tas bort eftersom den innehåller projekt. Flytta eller ta bort projekten först, eller arkivera mappen.",
                    BlazorMHD.UI.Core.DesignSystem.MhdState.Warning);
                return;
            }

            // Mapp med arkiverade projekt
            if (projects.Any(p => p.IsArchived))
            {
                MHD.MessageOk(blockedTitle,
                    "Mappen kan inte tas bort eftersom den innehåller arkiverade projekt. Visa arkiverade objekt eller arkivera mappen i stället.",
                    BlazorMHD.UI.Core.DesignSystem.MhdState.Warning);
                return;
            }

            // Helt tom mapp – bekräftelsedialog krävs innan borttagning.
            ShowDeleteFolderConfirmation(folder);
        }

        private void ShowDeleteFolderConfirmation(FolderMVVM folder)
        {
            var model = new BlazorMHD.UI.Core.Services.MhdDialogModel
            {
                Title = "Ta bort mapp?",
                State = BlazorMHD.UI.Core.DesignSystem.MhdState.Danger,
                Size = BlazorMHD.UI.Core.Services.MhdDialogSize.Medium,
                CloseOnOverlayClick = false,
                Content = builder =>
                {
                    builder.OpenElement(0, "p");
                    builder.AddAttribute(1, "class", "text-sm leading-relaxed text-slate-700 dark:text-slate-300");
                    builder.AddContent(2, $"Du håller på att ta bort mappen \"{folder.Name}\". Denna åtgärd kan inte ångras. Vill du fortsätta?");
                    builder.CloseElement();
                },
                Buttons =
                {
                    new BlazorMHD.UI.Core.Services.MhdDialogButtonModel
                    {
                        Text = "Avbryt",
                        State = BlazorMHD.UI.Core.DesignSystem.MhdState.Secondary,
                        IsPrimary = false,
                        OnClick = EventCallback.Factory.Create(this, () => Modal.CloseAsync())
                    },
                    new BlazorMHD.UI.Core.Services.MhdDialogButtonModel
                    {
                        Text = "Ta bort",
                        State = BlazorMHD.UI.Core.DesignSystem.MhdState.Danger,
                        IsPrimary = true,
                        OnClick = EventCallback.Factory.Create(this, () => ConfirmDeleteFolderAsync(folder))
                    }
                }
            };

            Modal.Show(model);
        }

        private async Task ConfirmDeleteFolderAsync(FolderMVVM folder)
        {
            bool ok = await Repo.Folder.DeleteAsync(folder.Id);
            if (!ok)
            {
                // Bekräftelsedialogen stängs automatiskt (AutoClose); visa felmeddelande.
                MHD.Notifications(ToastType.Delete, isSuccess: false);
                return;
            }

            // Uppdatera vänsterträdet.
            UoWService.Folder.State.RemoveFolder(folder);

            // Uppdatera högerpanelen – om den borttagna mappen var vald, gå till
            // Hela avdelningen (mappar är platta, ingen överordnad mapp finns).
            if (Folder.State.FolderSelected?.Id == folder.Id)
                await SelectWholeDepartmentAsync();

            // Successmeddelande: "Mappen har tagits bort."
            MHD.ToastMessage(folder.Name, ToastType.Delete, isSuccess: true);

            await InvokeAsync(StateHasChanged);
        }

        private async Task ContextProject(FolderMVVM folder, ListProjectMVVM project)
        {
            List<MhdContextMenuItem> list = [];

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var access = ContextMenuAccessPolicy.ForProject(authState.User, project.Access, IsFolderManageBlocked(folder));

            // Grouped order (separated by dividers), identical to the right-panel project list:
            // [skapa/importera] · [arbeta med projekt] · [livscykel] · [export] · [ta bort].
            if (access.CanManageLifecycle)
            {
                // Grupp 1 — skapa/importera
                list.Add(new() { IconHtml = Icons.Plus, Label = AppLoc[LocalizerConst.New, CalcResource.calculation], OnClickAsync = async () => await CreateCalcFromProjectTreeAsync(folder, project) });
                // Import a calculation copy (.atacost) into this project — creates a new calculation.
                list.Add(new() { IconHtml = Icons.ImportFromFile, Label = "Importera kalkyl...", OnClickAsync = () => RequestImportCalcCopy(folder, project, access.CanManageLifecycle) });
            }

            if (access.CanView)
            {
                // Grupp 2 — arbeta med projekt
                if (list.Count > 0)
                    list.Add(new() { IsSeparator = true });
                list.Add(new()
                {
                    IconHtml = access.CanEditWork ? Icons.Edit : Icons.Details,
                    Label = access.CanEditWork ? "Ändra projektuppgifter" : "Visa projekt",
                    OnClickAsync = () => { EditProjectFromTree(folder, project, access.CanEditWork); return Task.CompletedTask; }
                });
                list.Add(new() { IconHtml = Icons.Tender, Label = ResourceLoc.tender, OnClickAsync = () => { OpenProjectBidsFromTree(project); return Task.CompletedTask; } });
                list.Add(new()
                {
                    IconHtml = Icons.PermissionShield,
                    Label = access.CanManageSharing ? "Delning och behörighet" : "Visa delning och behörighet",
                    OnClickAsync = () => { OpenProjectShareFromTree(project, readOnly: !access.CanManageSharing); return Task.CompletedTask; }
                });
            }

            if (access.CanManageLifecycle)
            {
                // Grupp 3 — projektlivscykel
                list.Add(new() { IsSeparator = true });
                list.Add(new() { IconHtml = Icons.Folder, Label = AppLoc["moveProject"], OnClickAsync = () => { OpenMoveCopyProjectDialog(folder, project, MoveCopyOperation.Move); return Task.CompletedTask; } });
                list.Add(new() { IconHtml = Icons.Copy, Label = AppLoc["copyProject"], OnClickAsync = () => { OpenMoveCopyProjectDialog(folder, project, MoveCopyOperation.Copy); return Task.CompletedTask; } });

                if (!project.IsArchived)
                    list.Add(new() { IconHtml = Icons.Archive, Label = AppLoc["archiveProject"], OnClickAsync = async () => await ArchiveOrRestoreProjectAsync(project, archive: true) });
                else
                    list.Add(new() { IconHtml = Icons.Restore, Label = AppLoc["restoreFromArchive"], OnClickAsync = async () => await ArchiveOrRestoreProjectAsync(project, archive: false) });

                // Grupp 4 — export. Separator före, och en till mellan export och borttagning.
                list.Add(new() { IsSeparator = true });

                // Export the project as an .atacost copy — same dialog as the right-panel project list.
                list.Add(new() { IconHtml = Icons.Tender, Label = "Exportera projektet...", OnClickAsync = () => RequestExportProjectCopy(project, access.CanManageLifecycle) });

                // Grupp 5 — destruktiv åtgärd: "Ta bort projekt" ligger alltid sist, med separator före.
                list.Add(new() { IsSeparator = true });

                list.Add(new()
                {
                    IconHtml = Icons.Delete,
                    Label = ProjectDeleteHelper.DeleteLabel,
                    CssClass = ProjectDeleteHelper.DeleteCssClass,
                    OnClickAsync = () => ProjectDeleteHelper.RequestDeleteAsync(
                        this, Repo, MHD, ClientLog, project, access.CanManageLifecycle,
                        () => DeleteProjectFromTreeConfirmedAsync(folder, project))
                });
            }

            await ContextService.ShowMenuAsync(list);
        }

        private async Task DeleteProjectFromTreeConfirmedAsync(FolderMVVM folder, ListProjectMVVM project)
        {
            bool ok = await Repo.Project.DeleteAsync(project.Id);
            if (!ok)
            {
                // Bekräftelsedialogen stängs automatiskt (AutoClose); visa felmeddelande.
                MHD.Notifications(ToastType.Delete, isSuccess: false);
                return;
            }

            // Uppdatera vänsterträdet.
            folder.Projects?.Remove(project);

            // Uppdatera högerpanelen – om det borttagna projektet var valt, gå tillbaka
            // till mappens projektlista.
            if (Folder.State.ProjectSelected?.Id == project.Id)
                await SelectFolder(folder);

            UoWService.Folder.State.Notify();

            // Successmeddelande: "Projektet har tagits bort."
            MHD.ToastMessage(project.Name, ToastType.Delete, isSuccess: true);

            await InvokeAsync(StateHasChanged);
        }

        private async Task ContextCalc(FolderMVVM folder, ListProjectMVVM project, ListCalculationMVVM cal)
        {
            List<MhdContextMenuItem> list = [];

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var blocked = IsFolderManageBlocked(folder);
            var access = ContextMenuAccessPolicy.ForCalculation(authState.User, cal.Access, blocked);

            // Kalkylen öppnas redan genom klick/markering i trädet; context-menyn börjar därför
            // direkt med formuläråtgärden och behåller övriga behörighetsstyrda val.
            list.Add(new()
            {
                IconHtml = access.CanEditWork && cal.IsCurrentVersion ? Icons.Edit : Icons.Details,
                Label = access.CanEditWork && cal.IsCurrentVersion ? "Ändra kalkyluppgifter" : "Visa kalkyl",
                OnClickAsync = () => { EditCalculationFromTree(project, cal, access.CanEditWork && cal.IsCurrentVersion); return Task.CompletedTask; }
            });
            // "Visa detaljer" borttagen – öppna/ändra räcker. Kalkyldelning hanteras på projektnivå
            // (ingen "Delning och behörighet" i kalkylmenyn).

            if (access.CanManageLifecycle)
            {
                // Grupp 1 — huvudåtgärder (Kalkylversioner direkt efter Ändra)
                list.Add(new() { IsSeparator = true });
                list.Add(new() { IconHtml = Icons.Copy, Label = "Kalkylversioner", OnClickAsync = () => { OpenCalculationVersionsFromTree(project, cal); return Task.CompletedTask; } });

                // Grupp 2 — livscykel/hantering
                list.Add(new() { IsSeparator = true });
                // Delning sker på projektnivå – ingen separat kalkyldelning i kalkylmenyn (se ProjectShareUI).
                list.Add(new() { IconHtml = Icons.Folder, Label = AppLoc["moveCalculation"], OnClickAsync = () => { OpenMoveCopyCalcDialog(folder, project, cal, MoveCopyOperation.Move); return Task.CompletedTask; } });
                list.Add(new() { IconHtml = Icons.Copy, Label = AppLoc["copyCalculation"], OnClickAsync = () => { OpenMoveCopyCalcDialog(folder, project, cal, MoveCopyOperation.Copy); return Task.CompletedTask; } });
                list.Add(new() { IconHtml = Icons.Archive, Label = AppLoc["archiveCalculation"], OnClickAsync = async () => await ArchiveCalculationFromTreeAsync(project, cal) });
            }

            var projectCalcs = project.Calculations ?? [];

            if (access.CanManageLifecycle)
            {
                // Grupp 3 — export (separator före).
                list.Add(new() { IsSeparator = true });
                // Export the calculation as an .atacost copy — same dialog as the right-panel calculation list.
                list.Add(new() { IconHtml = Icons.Tender, Label = "Exportera kalkyl...", OnClickAsync = () => RequestExportCalcCopy(cal, access.CanManageLifecycle) });

                // Grupp 4 — destruktiv åtgärd: "Ta bort kalkyl" ligger alltid sist, med separator före.
                list.Add(new() { IsSeparator = true });

                list.Add(new()
                {
                    IconHtml = Icons.Delete,
                    Label = CalculationDeleteHelper.DeleteLabel(projectCalcs, cal),
                    CssClass = CalculationDeleteHelper.DeleteCssClass,
                    OnClickAsync = () => CalculationDeleteHelper.RequestDeleteAsync(
                        this, Repo, MHD, ClientLog, cal, projectCalcs, access.CanManageLifecycle,
                        () => DeleteCalculationFromTreeConfirmedAsync(folder, project, cal))
                });
            }

            await ContextService.ShowMenuAsync(list);
        }

        private async Task DeleteCalculationFromTreeConfirmedAsync(FolderMVVM folder, ListProjectMVVM project, ListCalculationMVVM cal)
        {
            bool ok = await Repo.Calculation.DeleteAsync(cal.Id);
            if (!ok)
            {
                MHD.Notifications(ToastType.Delete, isSuccess: false);
                return;
            }

            // Stäng öppen kalkyl om det var den som togs bort.
            if (Folder.State.Calculation?.Id == cal.Id)
                Folder.State.SetCalculation(null, project, folder);

            // Uppdatera vänsterträdet – ladda om projektets kalkyler.
            project.CalculationsLoaded = false;
            await Folder.SetCalcsToProject(project);
            UoWService.Folder.State.Notify();

            // Successmeddelande: "Kalkylen har tagits bort."
            MHD.ToastMessage(cal.Name, ToastType.Delete, isSuccess: true);

            await InvokeAsync(StateHasChanged);
        }

        private async Task ArchiveOrRestoreFolderAsync(FolderMVVM folder, bool archive)
        {
            if (archive)
            {
                var hasActive = (await Repo.Project.GetByFolderIdAsync(folder.Id, includeArchived: false)).Count > 0;
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
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge,
                DialogButtonsHelper.CreateSaveCancelButtons(ProjectForm.DialogFormId));
        }

        private async Task CreateCalcFromProjectTreeAsync(FolderMVVM folder, ListProjectMVVM project)
        {
            // Select the project (right panel) without toggling its expand state;
            // expansion is handled once the calculation has actually been created.
            await SelectProject(folder, project);

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
                                // The project now has a calculation — expand it so the new row shows.
                                project.ShowCalculations = ProjectHasChildren(project);
                                UoWService.Folder.State.Notify();
                            }
                            await Modal.CloseAsync();
                        })
                },
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge,
                DialogButtonsHelper.CreateSaveCancelButtons(CalculationFormUI.DialogFormId));
        }

        private void EditProjectFromTree(FolderMVVM folder, ListProjectMVVM project, bool canEdit = true)
        {
            var readOnly = !canEdit;
            var parameters = new Dictionary<string, object>
            {
                [nameof(ProjectForm.Project)] = project,
                [nameof(ProjectForm.FolderId)] = folder.Id,
                [nameof(ProjectForm.Callback)] = EventCallback.Factory.Create<Tuple<bool, ListProjectMVVM>>(this,
                    async t => await OnProjectEditedFromTreeAsync(folder, t))
            };

            if (readOnly)
            {
                Modal.ShowComponent<ProjectForm>(
                    "Visa projekt",
                    parameters,
                    BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge);
                return;
            }

            Modal.ShowComponent<ProjectForm>(
                AppLoc[LocalizerConst.Update, project.Name],
                parameters,
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge,
                DialogButtonsHelper.CreateSaveCancelButtons(ProjectForm.DialogFormId));
        }

        private async Task OnProjectEditedFromTreeAsync(FolderMVVM folder, Tuple<bool, ListProjectMVVM>? result)
        {
            if (result is null) { await Modal.CloseAsync(); return; }
            folder.Projects = await Repo.Project.GetByFolderIdAsync(folder.Id, UoWService.Folder.ShowArchived);
            if (folder.Projects != null)
                folder.Projects = folder.Projects.OrderByDescending(x => x.Order).ToList();
            UoWService.Folder.State.Notify();
            await Modal.CloseAsync();
        }

        private void EditCalculationFromTree(ListProjectMVVM project, ListCalculationMVVM cal, bool canEdit = true)
        {
            var readOnly = !canEdit;
            var parameters = new Dictionary<string, object>
            {
                [nameof(CalculationFormUI.Calculation)] = cal,
                [nameof(CalculationFormUI.Callback)] = EventCallback.Factory.Create<ListCalculationMVVM?>(this,
                    async updated =>
                    {
                        if (updated != null)
                        {
                            // Ladda om projektets kalkyler från servern så att Senast ändrad/Ändrad av,
                            // status och ändringsindikator/tooltip uppdateras direkt i både träd och
                            // högerpanel (inte bara namn/kod/status). Spegel av projekt-flödet
                            // (OnProjectEditedFromTreeAsync) – Notify() driver båda vyerna.
                            project.CalculationsLoaded = false;
                            await Folder.SetCalcsToProject(project);
                            project.ShowCalculations = ProjectHasChildren(project);
                            UoWService.Folder.State.Notify();
                        }
                        await Modal.CloseAsync();
                    }),
                // Refresh the project's calculations in the tree without closing the dialog
                // (used after approve-and-lock and after creating a contract/production calc).
                [nameof(CalculationFormUI.OnReloadList)] = EventCallback.Factory.Create(this,
                    async () =>
                    {
                        project.CalculationsLoaded = false;
                        await Folder.SetCalcsToProject(project);
                        project.ShowCalculations = ProjectHasChildren(project);
                        UoWService.Folder.State.Notify();
                    })
            };

            if (readOnly)
            {
                Modal.ShowComponent<CalculationFormUI>(
                    "Visa kalkyl",
                    parameters,
                    BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge);
                return;
            }

            Modal.ShowComponent<CalculationFormUI>(
                AppLoc[LocalizerConst.Update, cal.Name],
                parameters,
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge,
                DialogButtonsHelper.CreateSaveCancelButtons(CalculationFormUI.DialogFormId));
        }

        private void OpenProjectShareFromTree(ListProjectMVVM project, bool readOnly = false) =>
            Modal.ShowComponent<ProjectShareUI>(
                readOnly ? "Visa delning och behörighet" : "Delning och behörighet",
                new Dictionary<string, object>
                {
                    [nameof(ProjectShareUI.ProjectId)] = project.Id,
                    [nameof(ProjectShareUI.ProjectName)] = project.Name,
                    [nameof(ProjectShareUI.ProjectDepartmentId)] = project.DepartmentId!,
                    [nameof(ProjectShareUI.ProjectResponsible)] = project.Responsible,
                    [nameof(ProjectShareUI.ReadOnly)] = readOnly
                },
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge);

        private void OpenCalculationVersionsFromTree(ListProjectMVVM project, ListCalculationMVVM calculation) =>
            Modal.ShowComponent<CalculationVersionsDialog>(
                AppLoc["versions"].Value,
                new Dictionary<string, object>
                {
                    [nameof(CalculationVersionsDialog.Calculation)] = calculation,
                    [nameof(CalculationVersionsDialog.OnReloadRequired)] = EventCallback.Factory.Create(this,
                        async () =>
                        {
                            project.CalculationsLoaded = false;
                            await Folder.SetCalcsToProject(project);
                            project.ShowCalculations = ProjectHasChildren(project);
                            UoWService.Folder.State.Notify();
                        })
                },
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge);

        private void OpenProjectBidsFromTree(ListProjectMVVM project) =>
            Modal.ShowComponent<ProjectBidsDialog>(
                ResourceLoc.tender,
                new Dictionary<string, object>
                {
                    [nameof(ProjectBidsDialog.ProjectId)] = project.Id,
                    [nameof(ProjectBidsDialog.ProjectName)] = project.Name
                },
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge);

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
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge);

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
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge);

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
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge);

        private async Task RefreshAfterMoveCopyAsync()
        {
            Folder.State.ClearSelection();
            await UoWService.Folder.LoadPrivateAndGroupFoldersAsync();
        }

        // ---- External copy (.atacost) export/import from the folder tree ---------
        // These reuse the exact dialogs from the right-panel project/calculation
        // lists so behaviour stays consistent between the tree and the right panel.

        // Export a project copy (.atacost) — same dialog as the right-panel project list.
        private void OpenExportProjectDialog(ListProjectMVVM project) =>
            Modal.ShowComponent<SendProjectCopyDialog>(
                "Exportera en kopia av projektet",
                new Dictionary<string, object>
                {
                    [nameof(SendProjectCopyDialog.ProjectId)]   = project.Id,
                    [nameof(SendProjectCopyDialog.ProjectName)] = project.Name
                },
                BlazorMHD.UI.Core.Services.MhdDialogSize.Large);

        // Export a calculation copy (.atacost) — same dialog as the right-panel calculation list.
        private void OpenExportCalcDialog(ListCalculationMVVM cal) =>
            Modal.ShowComponent<SendCalculationCopyDialog>(
                "Exportera en kopia av kalkylen",
                new Dictionary<string, object>
                {
                    [nameof(SendCalculationCopyDialog.CalcId)]        = cal.Id,
                    [nameof(SendCalculationCopyDialog.CalcName)]      = cal.Name,
                    [nameof(SendCalculationCopyDialog.CalcTypeLabel)] = cal.CalculationType.ToString(),
                    [nameof(SendCalculationCopyDialog.VersionNumber)] = cal.VersionNumber,
                    [nameof(SendCalculationCopyDialog.IsPrivate)]     = cal.IsPrivate
                },
                BlazorMHD.UI.Core.Services.MhdDialogSize.Large);

        // Import a project copy (.atacost) into the given folder — target shown read-only.
        private void OpenImportProjectCopyDialog(FolderMVVM folder) =>
            Modal.ShowComponent<ImportCopyDialog>(
                "Importera projekt",
                new Dictionary<string, object>
                {
                    [nameof(ImportCopyDialog.Kind)]           = ProjectManagement.Shared.DTO.Transfer.AtacostPackageDTO.KindProject,
                    [nameof(ImportCopyDialog.PresetFolderId)] = folder.Id,
                    [nameof(ImportCopyDialog.TargetSummary)]  = BuildFolderTargetSummary(folder),
                    [nameof(ImportCopyDialog.OnCompleted)]    = EventCallback.Factory.Create(this, () => ReloadAfterImportAsync(folder, null))
                },
                BlazorMHD.UI.Core.Services.MhdDialogSize.Large);

        // Import a calculation copy (.atacost) into the given project — target shown read-only.
        private void OpenImportCalcCopyDialog(FolderMVVM folder, ListProjectMVVM project) =>
            Modal.ShowComponent<ImportCopyDialog>(
                "Importera kalkyl",
                new Dictionary<string, object>
                {
                    [nameof(ImportCopyDialog.Kind)]            = ProjectManagement.Shared.DTO.Transfer.AtacostPackageDTO.KindCalculation,
                    [nameof(ImportCopyDialog.PresetProjectId)] = project.Id,
                    [nameof(ImportCopyDialog.TargetSummary)]   = BuildProjectTargetSummary(folder, project),
                    [nameof(ImportCopyDialog.OnCompleted)]     = EventCallback.Factory.Create(this, () => ReloadAfterImportAsync(folder, project))
                },
                BlazorMHD.UI.Core.Services.MhdDialogSize.Large);

        private Task RequestImportProjectCopy(FolderMVVM folder, bool hasPermission)
        {
            if (hasPermission)
                OpenImportProjectCopyDialog(folder);
            else
                MHD.MessageOk("Importera projekt", "Du saknar behörighet att importera ett projekt till den här mappen.", BlazorMHD.UI.Core.DesignSystem.MhdState.Warning);

            return Task.CompletedTask;
        }

        private Task RequestImportCalcCopy(FolderMVVM folder, ListProjectMVVM project, bool hasPermission)
        {
            if (hasPermission)
                OpenImportCalcCopyDialog(folder, project);
            else
                MHD.MessageOk("Importera kalkyl", "Du saknar behörighet att importera en kalkyl till detta projekt.", BlazorMHD.UI.Core.DesignSystem.MhdState.Warning);

            return Task.CompletedTask;
        }

        private Task RequestExportProjectCopy(ListProjectMVVM project, bool hasPermission)
        {
            if (hasPermission)
                OpenExportProjectDialog(project);
            else
                MHD.MessageOk("Exportera projektet", "Du saknar behörighet att exportera detta projekt.", BlazorMHD.UI.Core.DesignSystem.MhdState.Warning);

            return Task.CompletedTask;
        }

        private Task RequestExportCalcCopy(ListCalculationMVVM calculation, bool hasPermission)
        {
            if (hasPermission)
                OpenExportCalcDialog(calculation);
            else
                MHD.MessageOk("Exportera kalkyl", "Du saknar behörighet att exportera denna kalkyl.", BlazorMHD.UI.Core.DesignSystem.MhdState.Warning);

            return Task.CompletedTask;
        }

        private string? GetSelectedDepartmentName() =>
            Folder.State.Departments.FirstOrDefault(d => d.Id == Folder.State.SelectedDepartmentId)?.Name;

        // "Department / Folder" — read-only target summary for a project import.
        private string BuildFolderTargetSummary(FolderMVVM folder)
        {
            var parts = new[] { GetSelectedDepartmentName(), folder.Name }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join(" / ", parts);
        }

        // "Department / Folder / Project" — read-only target summary for a calculation import.
        private string BuildProjectTargetSummary(FolderMVVM folder, ListProjectMVVM project)
        {
            var parts = new[] { GetSelectedDepartmentName(), folder.Name, project.Name }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join(" / ", parts);
        }

        // Refresh the tree (and right panel) after a successful import from the tree.
        private async Task ReloadAfterImportAsync(FolderMVVM folder, ListProjectMVVM? project)
        {
            if (project is not null)
            {
                // Calculation imported into an existing project — reload its calculations.
                project.CalculationsLoaded = false;
                await Folder.SetCalcsToProject(project);
                project.ShowCalculations = ProjectHasChildren(project);
            }
            else
            {
                // Project imported into a folder — reload the folder's projects.
                folder.ProjectsLoaded = false;
                await Folder.SetProjectsToFolder(folder);
                folder.ShowProjects = FolderHasChildren(folder);
            }

            UoWService.Folder.State.Notify();
            await InvokeAsync(StateHasChanged);
        }
    }
}
