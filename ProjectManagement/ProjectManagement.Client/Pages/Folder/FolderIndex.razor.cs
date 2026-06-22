using BlazorMHD.UI.Components.Data.DropdownPanel;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Pages.Project.ProjectPages;
using ProjectManagement.Client.Shared.Model.Project;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.ResourceFiles;
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

        private bool HasDepartmentAccess => Folder.State.Departments?.Any() == true;

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
                            $"FolderIndex.InitializeSession: authenticated but tenant/user claims missing (tenant={tenantId}, user={userId}); refreshing session.");
                        Nav.NavigateTo(AuthRecoveryPathHelper.BuildRefreshUrl(currentLocalUrl), forceLoad: true);
                        return;
                    }

                    await ClientLog.ErrorAsync(
                        $"FolderIndex.InitializeSession: tenant/user claims still missing after refresh (tenant={tenantId}, user={userId}).");
                    _initState = SessionInitState.Failed;
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                CurrentUserDepartmentId = GetUserDepartmentId(user);
                CanChooseAllDepartments =
                    user.IsInRole(PMRolesConst.Tenant.Admin) ||
                    user.IsInRole(PMRolesConst.Tenant.Manger);

                var departments = await GetAllowedDepartmentsAsync();
                Folder.State.SetDepartments(departments);

                SelectedDepartmentId = GetInitialDepartmentId(departments);

                _initState = SessionInitState.Ready;
                await LoadSelectedDepartmentAsync();

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

        private static int? GetClaimInt(ClaimsPrincipal user, string claimType) =>
            int.TryParse(user.FindFirst(claimType)?.Value, out var value) && value > 0 ? value : null;

        private async Task<List<ListDTO>> GetAllowedDepartmentsAsync()
        {
            var departments = await Repo.Departments.GetDepartmentsAsListAsync() ?? [];

            if (CanChooseAllDepartments)
                return departments;

            if (!CurrentUserDepartmentId.HasValue)
                return [];

            return departments
                .Where(x => x.Id == CurrentUserDepartmentId.Value)
                .ToList();
        }

        private int? GetInitialDepartmentId(List<ListDTO> departments)
        {
            if (departments.Count == 0)
                return null;

            if (CurrentUserDepartmentId.HasValue &&
                departments.Any(x => x.Id == CurrentUserDepartmentId.Value))
                return CurrentUserDepartmentId.Value;

            if (CanChooseAllDepartments)
                return departments[0].Id;

            return departments.Count == 1
                ? departments[0].Id
                : null;
        }

        private async Task OnDepartmentChanged(ChangeEventArgs e)
        {
            SelectedDepartmentId = int.TryParse(e.Value?.ToString(), out int id) && id > 0
                ? id
                : null;

            await LoadSelectedDepartmentAsync();
        }

        private async Task LoadSelectedDepartmentAsync(bool preserveOpenNodes = false)
        {
            var openNodes = preserveOpenNodes ? SnapshotOpenNodes() : null;

            Folder.State.SetSelectedDepartment(SelectedDepartmentId);
            Folder.State.ClearSelection();
            Folder.State.ClearFolders();

            if (!SelectedDepartmentId.HasValue)
            {
                Folder.State.SetOtherDepartment(false);
                return;
            }

            bool isOwnDepartment =
                CurrentUserDepartmentId.HasValue &&
                SelectedDepartmentId.Value == CurrentUserDepartmentId.Value;

            bool canManageSelectedDepartment = CanChooseAllDepartments || isOwnDepartment;
            Folder.State.SetOtherDepartment(!canManageSelectedDepartment);

            _isTreeLoading = true;
            await InvokeAsync(StateHasChanged);
            try
            {
                if (isOwnDepartment)
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

        private async Task OnTreeGroupingModeChanged(ChangeEventArgs e)
        {
            TreeGroupingMode = e.Value?.ToString() switch
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

        private async Task OnTreeSortModeChanged(ChangeEventArgs e)
        {
            var defaultSort = TreeGroupingMode == ProjectTreeGroupingMode.FolderStructure
                ? ProjectTreeSortMode.Manual
                : ProjectTreeSortMode.ModifiedNewest;
            TreeSortMode = NormalizeTreeSortMode(e.Value?.ToString(), defaultSort);
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
