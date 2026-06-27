using BlazorMHD.UI.Components.Data.DropdownPanel;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Pages.Project.ProjectPages;
using ProjectManagement.Client.Shared.Model.Project;
using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Client.Shared.SharedComponent;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.Helper;
using System.Globalization;
using System.Security.Claims;

namespace ProjectManagement.Client.Pages.Folder
{
    public partial class FolderIndex : IDisposable
    {
        bool SideBarVisible { get; set; } = true;
        bool CanChooseAllDepartments { get; set; }
        int? CurrentUserDepartmentId { get; set; }
        int? SelectedDepartmentId { get; set; }

        // Departments where the user has NO normal access but has shared/assigned projects (👥).
        private readonly HashSet<int> _sharedOnlyDeptIds = new();
        string TreeGroupingMode { get; set; } = ProjectTreeGroupingMode.FolderStructure;
        string TreeSortMode { get; set; } = ProjectTreeSortMode.Manual;

        private GroupSelectionInfo? _selectedGroupInfo;
        private bool _isReorderMode;
        private bool _isTreeLoading;

        // Tracks the post-login session/tenant bootstrap so the UI never shows the
        // empty "Inga avdelningar" / "Välj en mapp" states before user + tenant context is ready.
        private enum SessionInitState { NotStarted, Loading, Ready, Failed }
        private SessionInitState _initState = SessionInitState.NotStarted;

        private MhdDropdownPanel? _filterPanel;

        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        private const string TreeSortModeStorageKey = "ProjectTreeSortMode";
        private const string TreeGroupingModeStorageKey = "ProjectTree.TreeViewMode";
        private const string ShowArchivedStorageKey = "ProjectTree.ShowArchived";
        private const string SideBarCollapsedKey = "ProjectTreeSideBarCollapsed";
        private bool _sortPreferenceLoaded;

        private bool HasDepartmentAccess => Folder.State.Departments?.Count > 0;

        private int ActiveTreeFilterCount
        {
            get
            {
                var defaultSort = TreeGroupingMode == ProjectTreeGroupingMode.FolderStructure
                    ? ProjectTreeSortMode.Manual
                    : ProjectTreeSortMode.ModifiedNewest;
                return (TreeGroupingMode == ProjectTreeGroupingMode.FolderStructure ? 0 : 1) +
                       (TreeSortMode == defaultSort ? 0 : 1) +
                       (UoWService.Folder.ShowArchived ? 1 : 0);
            }
        }

        private string SelectedDepartmentValue =>
            SelectedDepartmentId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

        private sealed record OpenTreeSnapshot(HashSet<Guid> FolderIds, HashSet<Guid> ProjectIds);

        void ModalSeachForm() =>
            Modal.Show(new MhdDialogModel
            {
                Title = CalcLoc["searchFoldersProjectsCalcs"],
                Content = builder =>
                {
                    builder.OpenComponent(0, typeof(ProjectsSearch));
                    builder.AddAttribute(1, "CallBack",
                        EventCallback.Factory.Create(this, () => Modal.CloseAsync()));
                    builder.CloseComponent();
                },
                Buttons = [], Size = MhdDialogSize.ExtraLarge, IsDraggable = true, CloseOnOverlayClick = true
            });

        void ModalForm(FolderModel model) =>
            Modal.ShowComponent<FolderFormUI>(
                model.Id == Guid.Empty ? AppLoc[LocalizerConst.New, ResourceLoc.folder] : AppLoc[LocalizerConst.Update, model.Name],
                Icons.Folder,
                new Dictionary<string, object>
                {
                    [nameof(FolderFormUI.FolderForm)] = model,
                    [nameof(FolderFormUI.TargetDepartmentId)] = SelectedDepartmentId.GetValueOrDefault(),
                    [nameof(FolderFormUI.OnClickCallback)] = EventCallback.Factory.Create(this, (FolderModel f) => Folder.AddOrUpdateFolder(f))
                },
                MhdDialogSize.Large,
                DialogButtonsHelper.CreateSaveCancelButtons(FolderFormUI.DialogFormId));

        public void Dispose()
        {
            UoWService.Folder.State.OnChange -= Refresh;
            AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }

        public void Refresh()
        {
            if (_selectedGroupInfo != null &&
                (Folder.State.FolderSelected != null || Folder.State.ProjectSelected != null || Folder.State.Calculation != null))
            {
                _selectedGroupInfo = null;
            }
            InvokeAsync(StateHasChanged);
        }

        private Task OnGroupSelectedAsync(GroupSelectionInfo? info)
        {
            _selectedGroupInfo = info;
            if (info != null)
                Folder.State.ClearSelection();
            return Task.CompletedTask;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || _sortPreferenceLoaded)
                return;

            _sortPreferenceLoaded = true;

            try
            {
                var storedCollapsed = await JS.InvokeAsync<string?>("localStorage.getItem", SideBarCollapsedKey);
                if (storedCollapsed == "true")
                    SideBarVisible = false;

                var storedGroupingMode = await JS.InvokeAsync<string?>("localStorage.getItem", TreeGroupingModeStorageKey);
                if (storedGroupingMode == ProjectTreeGroupingMode.Projects)
                    TreeGroupingMode = ProjectTreeGroupingMode.Projects;

                var storedSortMode = await JS.InvokeAsync<string?>("localStorage.getItem", TreeSortModeStorageKey);
                var defaultSort = TreeGroupingMode == ProjectTreeGroupingMode.FolderStructure
                    ? ProjectTreeSortMode.Manual
                    : ProjectTreeSortMode.ModifiedNewest;
                TreeSortMode = NormalizeTreeSortMode(storedSortMode, defaultSort);

                if (TreeGroupingMode == ProjectTreeGroupingMode.Projects && TreeSortMode == ProjectTreeSortMode.Manual)
                    TreeSortMode = ProjectTreeSortMode.ModifiedNewest;

                var storedShowArchived = await JS.InvokeAsync<string?>("localStorage.getItem", ShowArchivedStorageKey);
                if (storedShowArchived == "true" && !UoWService.Folder.ShowArchived)
                {
                    Folder.ToggleArchivedFilter();
                    if (SelectedDepartmentId.HasValue)
                        await LoadSelectedDepartmentAsync(preserveOpenNodes: false);
                }
                else if (TreeGroupingMode == ProjectTreeGroupingMode.Projects && SelectedDepartmentId.HasValue)
                {
                    await EnsureProjectsLoadedForGroupingAsync();
                }

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                await ClientLog.ErrorAsync("Loading project tree preferences failed", ex: ex);
            }
        }

        private async Task SetSideBarVisible(bool visible)
        {
            SideBarVisible = visible;
            try
            {
                await JS.InvokeVoidAsync("localStorage.setItem", SideBarCollapsedKey, (!visible).ToString().ToLower());
            }
            catch { }
        }

        protected override async Task OnInitializedAsync()
        {
            UoWService.Folder.State.OnChange += Refresh;
            AuthenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;

            // Arriving via a notification deep-link: tell the tree not to restore its last localStorage
            // selection, so it can't override the explicit project/calculation this navigation will select.
            Folder.State.SuppressLastSelectionRestore = ReadOpenProjectId(Nav.Uri) is not null;

            await InitializeSessionAsync();
        }

        // Re-run the bootstrap when auth/tenant context becomes available after the first render
        // (e.g. after a silent revalidation), so the tree and lists load without a manual refresh.
        private void OnAuthenticationStateChanged(Task<AuthenticationState> authStateTask)
        {
            _ = InvokeAsync(async () =>
            {
                if (_initState is SessionInitState.Ready or SessionInitState.Loading)
                    return;

                await InitializeSessionAsync();
            });
        }

        /// <summary>
        /// Ordered post-login bootstrap: wait for authentication, read user + tenant context from the
        /// claims, then load departments → tree → workspace. Data is never requested (and an empty
        /// result is never committed as the final UI) before UserId and TenantId are known.
        /// </summary>
        private async Task InitializeSessionAsync()
        {
            _initState = SessionInitState.Loading;
            await InvokeAsync(StateHasChanged);

            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                ClaimsPrincipal user = authState.User;

                if (user.Identity?.IsAuthenticated != true)
                {
                    // Not authenticated yet — the router/[Authorize] handles the redirect. Reset to
                    // NotStarted (instead of caching an empty department list) so a later
                    // AuthenticationStateChanged re-runs this bootstrap once the user is signed in.
                    _initState = SessionInitState.NotStarted;
                    await ClientLog.InfoAsync("FolderIndex.InitializeSession: not authenticated yet; awaiting auth state.");
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                var tenantId = GetClaimInt(user, PMClaimsConst.Tenant);
                var userId = GetClaimInt(user, PMClaimsConst.UserId);

                // Authenticated, but the tenant/user claims are not present on the client principal yet.
                // This is exactly the "only works after a manual F5" state: the session was not fully
                // established for this render. Recover via /auth/refresh once (the same pattern the 401
                // handler uses) instead of rendering an empty "Inga avdelningar" workspace. The retry
                // flag guards against an infinite refresh loop.
                if (tenantId is null || userId is null)
                {
                    var currentLocalUrl = GetCurrentLocalUrl();
                    if (!AuthRecoveryPathHelper.HasRetryFlag(currentLocalUrl))
                    {
                        await ClientLog.InfoAsync(
                            $"FolderIndex.InitializeSession: authenticated but tenant/user claims missing (tenant={tenantId}, user={userId}); refreshing session once.");
                        Nav.NavigateTo(AuthRecoveryPathHelper.BuildRefreshUrl(currentLocalUrl), forceLoad: true);
                        return;
                    }

                    // Already refreshed once and the session still has no tenant/user claims: this is an
                    // authentication problem, not a workspace/data error. Send the user to login in a
                    // controlled way instead of showing "could not load workspace" or starting another
                    // retry (which would loop). Stay in a neutral loading state until the redirect lands.
                    await ClientLog.ErrorAsync(
                        $"FolderIndex.InitializeSession: tenant/user claims still missing after refresh (tenant={tenantId}, user={userId}); redirecting to login.");
                    _initState = SessionInitState.NotStarted;
                    Nav.NavigateTo(AuthRecoveryPathHelper.BuildLoginUrl(currentLocalUrl), forceLoad: true);
                    return;
                }

                CurrentUserDepartmentId = GetUserDepartmentId(user);
                // TenantUser is scoped to its own department. Extra project sharing may grant
                // project access, but it must not expose or unlock another department's folders.
                CanChooseAllDepartments = user.IsInRole(PMRolesConst.Tenant.Admin);

                var departments = await GetAllowedDepartmentsAsync();
                Folder.State.SetDepartments(departments);

                SelectedDepartmentId = GetInitialDepartmentId(departments);

                _initState = SessionInitState.Ready;
                await LoadSelectedDepartmentAsync();

                // Deep-link from a notification's "Open project": select the project in the tree.
                await TryOpenProjectFromQueryAsync();

                await ClientLog.InfoAsync(
                    $"FolderIndex.InitializeSession: ready (tenant={tenantId}, user={userId}, departments={departments.Count}, " +
                    $"selectedDepartment={SelectedDepartmentId}, canChooseAll={CanChooseAllDepartments}).");
            }
            catch (Exception ex)
            {
                _initState = SessionInitState.Failed;
                await ClientLog.ErrorAsync("FolderIndex.InitializeSession failed", ex: ex);
                await InvokeAsync(StateHasChanged);
            }
        }

        private string GetCurrentLocalUrl()
        {
            var uri = new Uri(Nav.Uri);
            return AuthRecoveryPathHelper.NormalizeLocalUrl($"{uri.PathAndQuery}{uri.Fragment}");
        }

        /// <summary>
        /// Handles the <c>?openProject={id}</c> deep-link produced by a notification's "Open project".
        /// Re-checks access on the server, switches to the project's department (when selectable), expands
        /// its folder and selects the project. Best-effort and fully guarded: if anything is missing
        /// (e.g. a shared project outside the user's selectable departments) the user simply lands on the
        /// workspace without a forced selection.
        /// </summary>
        private async Task TryOpenProjectFromQueryAsync()
        {
            Guid projectId;
            int? calculationId;
            try
            {
                var id = ReadOpenProjectId(Nav.Uri);
                if (id is null)
                {
                    Folder.State.SuppressLastSelectionRestore = false;
                    return;
                }
                projectId = id.Value;
                calculationId = ReadOpenCalcId(Nav.Uri);
            }
            catch
            {
                Folder.State.SuppressLastSelectionRestore = false;
                return;
            }

            try
            {
                var info = await Repo.Notification.GetOpenInfoAsync(projectId);
                if (info is null || !info.CanOpen || info.FolderId is null)
                {
                    await StripOpenProjectParamAsync();
                    return;
                }

                // Switch to the project's department only when the user may select it.
                if (info.DepartmentId.HasValue
                    && info.DepartmentId != SelectedDepartmentId
                    && Folder.State.Departments.Any(d => d.Id == info.DepartmentId.Value))
                {
                    SelectedDepartmentId = info.DepartmentId;
                    await LoadSelectedDepartmentAsync();
                }

                var folder = Folder.State.FoldersList.FirstOrDefault(f => f.Id == info.FolderId.Value);
                if (folder is not null)
                {
                    await Folder.SetProjectsToFolder(folder);
                    folder.ShowProjects = true;

                    var project = folder.Projects?.FirstOrDefault(p => p.Id == projectId);
                    if (project is not null)
                    {
                        // Always select the project explicitly first (never the folder's first project).
                        Folder.State.SelectProject(folder, project);

                        // Calculation notification: expand the project and open the exact calculation when
                        // it is still accessible (loaded into the shared list). Otherwise the project stays
                        // selected — the bell already re-checks access before navigating here.
                        if (calculationId is { } calcId)
                        {
                            await Folder.SetCalcsToProject(project);
                            var calc = project.Calculations?.FirstOrDefault(c => c.Id == calcId);
                            if (calc is not null)
                            {
                                project.ShowCalculations = true;
                                await CalcService.SetCalc(calc.Id, project, folder);
                            }
                        }
                    }
                }

                await StripOpenProjectParamAsync();
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                await ClientLog.ErrorAsync("Opening project from notification deep-link failed", ex: ex);
            }
            finally
            {
                // The explicit selection is applied; let the tree restore selections normally again.
                Folder.State.SuppressLastSelectionRestore = false;
            }
        }

        private static int? ReadOpenCalcId(string url)
        {
            var query = new Uri(url).Query;
            if (string.IsNullOrEmpty(query))
                return null;

            foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split('=', 2);
                if (pair.Length == 2
                    && pair[0].Equals("openCalc", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(Uri.UnescapeDataString(pair[1]), out var id)
                    && id > 0)
                {
                    return id;
                }
            }

            return null;
        }

        private static Guid? ReadOpenProjectId(string url)
        {
            var query = new Uri(url).Query;
            if (string.IsNullOrEmpty(query))
                return null;

            foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split('=', 2);
                if (pair.Length == 2
                    && pair[0].Equals("openProject", StringComparison.OrdinalIgnoreCase)
                    && Guid.TryParse(Uri.UnescapeDataString(pair[1]), out var id))
                {
                    return id;
                }
            }

            return null;
        }

        private Task StripOpenProjectParamAsync()
        {
            try
            {
                var uri = new Uri(Nav.Uri);
                if (!string.IsNullOrEmpty(uri.Query))
                    Nav.NavigateTo(uri.GetLeftPart(UriPartial.Path), replace: true);
            }
            catch
            {
                // Non-critical: leaving the query param only means a refresh re-opens the project.
            }

            return Task.CompletedTask;
        }

        private static int? GetClaimInt(ClaimsPrincipal user, string claimType) =>
            int.TryParse(user.FindFirst(claimType)?.Value, out var value) && value > 0 ? value : null;

        // Departments the user may pick: their normal-access department(s) (own dept, or all for Admin)
        // PLUS any department where projects are shared/assigned to them (👥). The shared-only ones are
        // tracked in _sharedOnlyDeptIds so the rest of the page can tell normal vs shared access.
        private async Task<List<ListDTO>> GetAllowedDepartmentsAsync()
        {
            var accessible = await Repo.Folder.GetAccessibleDepartmentsAsync() ?? [];

            _sharedOnlyDeptIds.Clear();
            foreach (var d in accessible.Where(x => x.SharedOnly))
                _sharedOnlyDeptIds.Add(d.Id);

            return accessible.Cast<ListDTO>().ToList();
        }

        private int? GetInitialDepartmentId(List<ListDTO> departments)
        {
            if (departments.Count == 0)
                return null;

            // Own normal department keeps today's behavior (lands on the user's department).
            if (CurrentUserDepartmentId.HasValue &&
                departments.Any(x => x.Id == CurrentUserDepartmentId.Value && !_sharedOnlyDeptIds.Contains(x.Id)))
                return CurrentUserDepartmentId.Value;

            if (CanChooseAllDepartments)
                return departments[0].Id;

            // A user without a normal department but with shared/assigned projects lands on
            // "Alla tillgängliga" so every project they may see is visible at once.
            return FolderConstants.AllAvailableDepartmentId;
        }

        // Options for the styled single-select department dropdown: normal departments (plain name),
        // shared-only departments (👥), and the "Alla tillgängliga" special entry.
        private IEnumerable<MhdSelectItem<int?>> DepartmentItems
        {
            get
            {
                foreach (var d in Folder.State.Departments)
                {
                    var sharedOnly = _sharedOnlyDeptIds.Contains(d.Id);
                    yield return new MhdSelectItem<int?>
                    {
                        Value = d.Id,
                        Label = sharedOnly ? $"{d.Name} 👥" : d.Name
                    };
                }

                if (Folder.State.Departments.Count > 0)
                    yield return new MhdSelectItem<int?>
                    {
                        Value = FolderConstants.AllAvailableDepartmentId,
                        Label = "Alla tillgängliga"
                    };
            }
        }

        // The dropdown is only disabled when there is genuinely a single option to pick.
        private bool IsDepartmentSelectDisabled => DepartmentItems.Count() <= 1;

        // Name of the currently selected scope, used by the right panel / report context line.
        // "Alla tillgängliga" for the special scope; otherwise the department name (with 👥 for shared).
        private string? SelectedScopeName =>
            Folder.State.AllAvailable
                ? "Alla tillgängliga"
                : Folder.State.Departments.FirstOrDefault(d => d.Id == SelectedDepartmentId) is { } dept
                    ? (_sharedOnlyDeptIds.Contains(dept.Id) ? $"{dept.Name} 👥" : dept.Name)
                    : null;

        private async Task OnDepartmentSelected(int? id)
        {
            SelectedDepartmentId = id is > 0 || id == FolderConstants.AllAvailableDepartmentId ? id : null;

            await LoadSelectedDepartmentAsync();
        }

        private async Task LoadSelectedDepartmentAsync(bool preserveOpenNodes = false)
        {
            var openNodes = preserveOpenNodes ? SnapshotOpenNodes() : null;

            bool isAllAvailable = SelectedDepartmentId == FolderConstants.AllAvailableDepartmentId;

            if (SelectedDepartmentId.HasValue && !isAllAvailable &&
                !Folder.State.Departments.Any(x => x.Id == SelectedDepartmentId.Value))
            {
                await ClientLog.InfoAsync(
                    $"FolderIndex.LoadSelectedDepartment: blocked unauthorized department selection ({SelectedDepartmentId}).");
                SelectedDepartmentId = null;
            }

            Folder.State.SetSelectedDepartment(SelectedDepartmentId);
            Folder.State.ClearSelection();
            Folder.State.ClearFolders();

            if (!SelectedDepartmentId.HasValue)
            {
                Folder.State.SetOtherDepartment(false);
                Folder.State.SetAllAvailable(false);
                return;
            }

            // "Alla tillgängliga": every folder the user can reach, grouped by department in the tree.
            // Read-only is decided PER folder (IsReadOnlyGroup), so the global OtherDepartment flag stays
            // false here while shared-department folders remain non-manageable.
            if (isAllAvailable)
            {
                Folder.State.SetAllAvailable(true);
                Folder.State.SetOtherDepartment(false);

                _isTreeLoading = true;
                await InvokeAsync(StateHasChanged);
                try
                {
                    await Folder.LoadAccessibleFoldersAsync();
                    if (openNodes is not null)
                        await RestoreOpenNodesAsync(openNodes);
                    await EnsureProjectsLoadedForGroupingAsync();
                }
                finally
                {
                    _isTreeLoading = false;
                    await InvokeAsync(StateHasChanged);
                }
                return;
            }

            Folder.State.SetAllAvailable(false);

            bool isSharedOnly = _sharedOnlyDeptIds.Contains(SelectedDepartmentId.Value);
            bool isOwnNormalDepartment =
                CurrentUserDepartmentId.HasValue &&
                SelectedDepartmentId.Value == CurrentUserDepartmentId.Value &&
                !isSharedOnly;

            // A 👥 department (shared only) is treated like "another department": folders are read-only
            // visual groups. Admins manage everything; everyone else only manages their own department.
            bool canManageSelectedDepartment = CanChooseAllDepartments || isOwnNormalDepartment;
            Folder.State.SetOtherDepartment(!canManageSelectedDepartment);

            _isTreeLoading = true;
            await InvokeAsync(StateHasChanged);
            try
            {
                if (isOwnNormalDepartment)
                    await Folder.LoadPrivateAndGroupFoldersAsync();
                else
                    await Folder.LoadFoldersByDepartmentAsync(SelectedDepartmentValue);

                if (openNodes is not null)
                    await RestoreOpenNodesAsync(openNodes);

                await EnsureProjectsLoadedForGroupingAsync();
            }
            finally
            {
                _isTreeLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task ToggleArchivedAsync()
        {
            Folder.ToggleArchivedFilter();
            try
            {
                await JS.InvokeVoidAsync("localStorage.setItem", ShowArchivedStorageKey, UoWService.Folder.ShowArchived.ToString().ToLower());
            }
            catch { }
            await LoadSelectedDepartmentAsync(preserveOpenNodes: true);
        }

        // Options for the styled single-select tree-view dropdown (mirrors the former <option> list).
        private static readonly IReadOnlyList<MhdSelectItem<string>> TreeGroupingItems = new[]
        {
            new MhdSelectItem<string> { Value = ProjectTreeGroupingMode.FolderStructure, Label = "Mappstruktur" },
            new MhdSelectItem<string> { Value = ProjectTreeGroupingMode.Projects, Label = "Projekt" },
        };

        // Options for the styled single-select sort dropdown. "Manuell ordning" is only offered in
        // folder-structure mode, matching the previous @if-guarded <option>.
        private IEnumerable<MhdSelectItem<string>> TreeSortItems
        {
            get
            {
                var items = new List<MhdSelectItem<string>>();
                if (TreeGroupingMode == ProjectTreeGroupingMode.FolderStructure)
                    items.Add(new MhdSelectItem<string> { Value = ProjectTreeSortMode.Manual, Label = "Manuell ordning" });
                items.Add(new MhdSelectItem<string> { Value = ProjectTreeSortMode.NameAscending, Label = "Namn A–Ö" });
                items.Add(new MhdSelectItem<string> { Value = ProjectTreeSortMode.NameDescending, Label = "Namn Ö–A" });
                items.Add(new MhdSelectItem<string> { Value = ProjectTreeSortMode.CreatedNewest, Label = "Skapad nyast först" });
                items.Add(new MhdSelectItem<string> { Value = ProjectTreeSortMode.CreatedOldest, Label = "Skapad äldst först" });
                items.Add(new MhdSelectItem<string> { Value = ProjectTreeSortMode.ModifiedNewest, Label = "Senast ändrad först" });
                items.Add(new MhdSelectItem<string> { Value = ProjectTreeSortMode.LastOpenedNewest, Label = "Senast öppnad först" });
                return items;
            }
        }

        private async Task OnTreeGroupingModeChanged(string value)
        {
            TreeGroupingMode = value switch
            {
                ProjectTreeGroupingMode.Projects => ProjectTreeGroupingMode.Projects,
                _ => ProjectTreeGroupingMode.FolderStructure
            };

            await SaveTreeGroupingModeAsync();

            if (TreeGroupingMode == ProjectTreeGroupingMode.Projects && TreeSortMode == ProjectTreeSortMode.Manual)
            {
                TreeSortMode = ProjectTreeSortMode.ModifiedNewest;
                await SaveTreeSortModeAsync();
            }

            await EnsureProjectsLoadedForGroupingAsync();
        }

        private async Task OnTreeSortModeChanged(string value)
        {
            var defaultSort = TreeGroupingMode == ProjectTreeGroupingMode.FolderStructure
                ? ProjectTreeSortMode.Manual
                : ProjectTreeSortMode.ModifiedNewest;
            TreeSortMode = NormalizeTreeSortMode(value, defaultSort);
            _isReorderMode = false;
            await SaveTreeSortModeAsync();
        }

        private async Task ResetTreeFiltersAsync()
        {
            var reloadNeeded = UoWService.Folder.ShowArchived;

            _isReorderMode = false;
            TreeGroupingMode = ProjectTreeGroupingMode.FolderStructure;
            TreeSortMode = ProjectTreeSortMode.Manual;
            await SaveTreeGroupingModeAsync();
            await SaveTreeSortModeAsync();

            if (reloadNeeded)
            {
                Folder.ToggleArchivedFilter();
                try { await JS.InvokeVoidAsync("localStorage.setItem", ShowArchivedStorageKey, "false"); } catch { }
                await LoadSelectedDepartmentAsync(preserveOpenNodes: true);
            }
        }

        private async Task StartReorderModeAsync()
        {
            _filterPanel?.ClosePanel();
            if (TreeSortMode != ProjectTreeSortMode.Manual)
            {
                TreeSortMode = ProjectTreeSortMode.Manual;
                await SaveTreeSortModeAsync();
            }
            _isReorderMode = true;
            await InvokeAsync(StateHasChanged);
        }

        private async Task ExitManualOrderAsync()
        {
            _isReorderMode = false;
            await InvokeAsync(StateHasChanged);
        }

        private async Task SaveTreeGroupingModeAsync()
        {
            try
            {
                await JS.InvokeVoidAsync("localStorage.setItem", TreeGroupingModeStorageKey, TreeGroupingMode);
            }
            catch (Exception ex)
            {
                await ClientLog.ErrorAsync("Saving project tree grouping mode failed", ex: ex);
            }
        }

        private async Task SaveTreeSortModeAsync()
        {
            try
            {
                await JS.InvokeVoidAsync("localStorage.setItem", TreeSortModeStorageKey, TreeSortMode);
            }
            catch (Exception ex)
            {
                await ClientLog.ErrorAsync("Saving project tree sort mode failed", ex: ex);
            }
        }

        private async Task EnsureProjectsLoadedForGroupingAsync()
        {
            if (TreeGroupingMode == ProjectTreeGroupingMode.FolderStructure || !SelectedDepartmentId.HasValue)
                return;

            foreach (var folder in Folder.State.FoldersList)
            {
                await Folder.SetProjectsToFolder(folder);

                foreach (var project in folder.Projects ?? [])
                {
                    await Folder.SetCalcsToProject(project);
                }
            }
        }

        private OpenTreeSnapshot SnapshotOpenNodes()
        {
            var folderIds = Folder.State.FoldersList
                .Where(folder => folder.ShowProjects)
                .Select(folder => folder.Id)
                .ToHashSet();

            var projectIds = Folder.State.FoldersList
                .SelectMany(folder => folder.Projects ?? [])
                .Where(project => project.ShowCalculations)
                .Select(project => project.Id)
                .ToHashSet();

            return new OpenTreeSnapshot(folderIds, projectIds);
        }

        private async Task RestoreOpenNodesAsync(OpenTreeSnapshot snapshot)
        {
            if (snapshot.FolderIds.Count == 0 && snapshot.ProjectIds.Count == 0)
                return;

            foreach (var folder in Folder.State.FoldersList)
            {
                if (!snapshot.FolderIds.Contains(folder.Id) && snapshot.ProjectIds.Count == 0)
                    continue;

                await Folder.SetProjectsToFolder(folder);
                var hasOpenProject = (folder.Projects ?? []).Any(project => snapshot.ProjectIds.Contains(project.Id));
                if (!snapshot.FolderIds.Contains(folder.Id) && !hasOpenProject)
                    continue;

                // Never restore an empty folder into an expanded state.
                folder.ShowProjects = (snapshot.FolderIds.Contains(folder.Id) || hasOpenProject)
                                      && (folder.Projects?.Count ?? 0) > 0;

                foreach (var project in folder.Projects ?? [])
                {
                    if (!snapshot.ProjectIds.Contains(project.Id))
                        continue;

                    await Folder.SetCalcsToProject(project);
                    // Only expand the project if it still has calculations to show.
                    project.ShowCalculations = (project.Calculations?.Count ?? 0) > 0;
                    if (project.ShowCalculations)
                        folder.ShowProjects = true;
                }
            }
        }

        private static int? GetUserDepartmentId(ClaimsPrincipal user) =>
            int.TryParse(user.FindFirst(PMClaimsConst.DepartmentId)?.Value, out int departmentId) && departmentId > 0
                ? departmentId
                : null;

        private static string NormalizeTreeSortMode(string? sortMode, string defaultMode = ProjectTreeSortMode.Manual) =>
            sortMode switch
            {
                ProjectTreeSortMode.NameAscending => ProjectTreeSortMode.NameAscending,
                ProjectTreeSortMode.NameDescending => ProjectTreeSortMode.NameDescending,
                ProjectTreeSortMode.CreatedNewest => ProjectTreeSortMode.CreatedNewest,
                ProjectTreeSortMode.CreatedOldest => ProjectTreeSortMode.CreatedOldest,
                ProjectTreeSortMode.ModifiedNewest => ProjectTreeSortMode.ModifiedNewest,
                ProjectTreeSortMode.LastOpenedNewest => ProjectTreeSortMode.LastOpenedNewest,
                ProjectTreeSortMode.Status => ProjectTreeSortMode.Status,
                ProjectTreeSortMode.StatusOrder => ProjectTreeSortMode.StatusOrder,
                ProjectTreeSortMode.Manual => ProjectTreeSortMode.Manual,
                _ => defaultMode
            };
    }
}
