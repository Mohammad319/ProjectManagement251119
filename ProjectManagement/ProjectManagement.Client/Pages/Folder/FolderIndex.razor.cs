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
        string TreeSortMode { get; set; } = ProjectTreeSortMode.NameAscending;
        bool KeepFolderStructure { get; set; } = true;

        [Inject] private IJSRuntime JS { get; set; } = default!;

        private const string TreeSortModeStorageKey = "ProjectTreeSortMode";
        private const string SideBarCollapsedKey = "ProjectTreeSideBarCollapsed";
        private bool _sortPreferenceLoaded;

        private bool HasDepartmentAccess => Folder.State.Departments?.Any() == true;

        private int ActiveTreeFilterCount =>
            (TreeGroupingMode == ProjectTreeGroupingMode.FolderStructure ? 0 : 1) +
            (TreeGroupingMode != ProjectTreeGroupingMode.FolderStructure && !KeepFolderStructure ? 1 : 0) +
            (TreeSortMode == ProjectTreeSortMode.NameAscending ? 0 : 1) +
            (UoWService.Folder.ShowArchived ? 1 : 0);

        private string SelectedDepartmentValue =>
            SelectedDepartmentId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

        private sealed record OpenTreeSnapshot(HashSet<Guid> FolderIds, HashSet<Guid> ProjectIds);

        void ModalSeachForm() =>
        //Modal.AddModal<ProjectsSearch>(
        //    "",
        //    new Dictionary<string, object>
        //    {
        //        [nameof(ProjectsSearch.CallBack)] = EventCallback.Factory.Create(this, Modal.ClearModal)
        //    });
        Modal.Show(new DialogModel
        {
            Title = AppLoc["searchProjects"],
            Content = builder =>
            {
                builder.OpenComponent(0, typeof(ProjectsSearch));
                builder.AddAttribute(1, "CallBack",
                    EventCallback.Factory.Create(this, () =>
                    {
                        // هنا ما كان Modal.ClearModal
                        // في النظام الجديد: نغلق الـ Dialog
                        Modal.Close();
                    }));
                builder.CloseComponent();
            },
            Buttons = [], Size = DialogSize.ExtraLarge, IsDraggable = true, CloseOnOverlayClick = true
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
                DialogSize.Large,
                DialogButtonsHelper.CreateSaveCancelButtons(FolderFormUI.DialogFormId));

        public void Dispose()
        {
            UoWService.Folder.State.OnChange -= Refresh;
        }

        public void Refresh() => InvokeAsync(StateHasChanged);

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || _sortPreferenceLoaded)
                return;

            _sortPreferenceLoaded = true;

            try
            {
                var storedSortMode = await JS.InvokeAsync<string?>("localStorage.getItem", TreeSortModeStorageKey);
                TreeSortMode = NormalizeTreeSortMode(storedSortMode);

                var storedCollapsed = await JS.InvokeAsync<string?>("localStorage.getItem", SideBarCollapsedKey);
                if (storedCollapsed == "true")
                    SideBarVisible = false;

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

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            ClaimsPrincipal user = authState.User;

            CurrentUserDepartmentId = GetUserDepartmentId(user);
            CanChooseAllDepartments =
                user.IsInRole(PMRolesConst.Tenant.Admin) ||
                user.IsInRole(PMRolesConst.Tenant.SuperManger);

            var departments = await GetAllowedDepartmentsAsync();
            Folder.State.SetDepartments(departments);

            SelectedDepartmentId = GetInitialDepartmentId(departments);
            await LoadSelectedDepartmentAsync();
        }

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

            if (isOwnDepartment)
                await Folder.LoadPrivateAndGroupFoldersAsync();
            else
                await Folder.LoadFoldersByDepartmentAsync(SelectedDepartmentValue);

            if (openNodes is not null)
                await RestoreOpenNodesAsync(openNodes);

            await EnsureProjectsLoadedForGroupingAsync();
        }

        private async Task ToggleArchivedAsync()
        {
            Folder.ToggleArchivedFilter();
            await LoadSelectedDepartmentAsync(preserveOpenNodes: true);
        }

        private async Task OnTreeGroupingModeChanged(ChangeEventArgs e)
        {
            TreeGroupingMode = e.Value?.ToString() switch
            {
                ProjectTreeGroupingMode.Year => ProjectTreeGroupingMode.Year,
                ProjectTreeGroupingMode.YearQuarter => ProjectTreeGroupingMode.YearQuarter,
                ProjectTreeGroupingMode.Status => ProjectTreeGroupingMode.Status,
                _ => ProjectTreeGroupingMode.FolderStructure
            };

            if (TreeGroupingMode != ProjectTreeGroupingMode.FolderStructure &&
                TreeSortMode == ProjectTreeSortMode.Manual)
            {
                TreeSortMode = ProjectTreeSortMode.NameAscending;
                await SaveTreeSortModeAsync();
            }

            await EnsureProjectsLoadedForGroupingAsync();
        }

        private void OnKeepFolderStructureChanged(ChangeEventArgs e)
        {
            KeepFolderStructure = e.Value is bool value && value;
        }

        private async Task OnTreeSortModeChanged(ChangeEventArgs e)
        {
            TreeSortMode = NormalizeTreeSortMode(e.Value?.ToString());
            await SaveTreeSortModeAsync();
        }

        private async Task ResetTreeFiltersAsync()
        {
            var reloadNeeded = UoWService.Folder.ShowArchived;

            TreeGroupingMode = ProjectTreeGroupingMode.FolderStructure;
            TreeSortMode = ProjectTreeSortMode.NameAscending;
            KeepFolderStructure = true;
            await SaveTreeSortModeAsync();

            if (reloadNeeded)
            {
                Folder.ToggleArchivedFilter();
                await LoadSelectedDepartmentAsync(preserveOpenNodes: true);
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

                folder.ShowProjects = snapshot.FolderIds.Contains(folder.Id) || hasOpenProject;

                foreach (var project in folder.Projects ?? [])
                {
                    if (!snapshot.ProjectIds.Contains(project.Id))
                        continue;

                    await Folder.SetCalcsToProject(project);
                    project.ShowCalculations = true;
                    folder.ShowProjects = true;
                }
            }
        }

        private static int? GetUserDepartmentId(ClaimsPrincipal user) =>
            int.TryParse(user.FindFirst(PMClaimsConst.DepartmentId)?.Value, out int departmentId) && departmentId > 0
                ? departmentId
                : null;

        private static string NormalizeTreeSortMode(string? sortMode) =>
            sortMode switch
            {
                ProjectTreeSortMode.NameDescending => ProjectTreeSortMode.NameDescending,
                ProjectTreeSortMode.CreatedNewest => ProjectTreeSortMode.CreatedNewest,
                ProjectTreeSortMode.CreatedOldest => ProjectTreeSortMode.CreatedOldest,
                ProjectTreeSortMode.ModifiedNewest => ProjectTreeSortMode.ModifiedNewest,
                ProjectTreeSortMode.LastOpenedNewest => ProjectTreeSortMode.LastOpenedNewest,
                ProjectTreeSortMode.Status => ProjectTreeSortMode.Status,
                ProjectTreeSortMode.Manual => ProjectTreeSortMode.Manual,
                _ => ProjectTreeSortMode.NameAscending
            };
    }
}
