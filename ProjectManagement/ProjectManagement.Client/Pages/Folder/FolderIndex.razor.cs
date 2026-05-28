using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Pages.Project.ProjectPages;
using ProjectManagement.Client.Shared.Model.Project;
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

        [Inject] private IJSRuntime JS { get; set; } = default!;

        private const string TreeSortModeStorageKey = "ProjectTreeSortMode";
        private bool _sortPreferenceLoaded;

        private bool HasDepartmentAccess => Folder.State.Departments?.Any() == true;

        private string SelectedDepartmentValue =>
            SelectedDepartmentId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

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
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                await ClientLog.ErrorAsync("Loading project tree sort mode failed", ex: ex);
            }
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

        private async Task LoadSelectedDepartmentAsync()
        {
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

            Folder.State.SetOtherDepartment(!isOwnDepartment);

            if (isOwnDepartment)
                await Folder.LoadPrivateAndGroupFoldersAsync();
            else
                await Folder.LoadFoldersByDepartmentAsync(SelectedDepartmentValue);

            await EnsureProjectsLoadedForGroupingAsync();
        }

        private async Task ToggleArchivedAsync()
        {
            Folder.ToggleArchivedFilter();
            await LoadSelectedDepartmentAsync();
        }

        private async Task OnTreeGroupingModeChanged(ChangeEventArgs e)
        {
            TreeGroupingMode = e.Value?.ToString() switch
            {
                ProjectTreeGroupingMode.Year => ProjectTreeGroupingMode.Year,
                ProjectTreeGroupingMode.Quarter => ProjectTreeGroupingMode.Quarter,
                _ => ProjectTreeGroupingMode.FolderStructure
            };

            await EnsureProjectsLoadedForGroupingAsync();
        }

        private async Task OnTreeSortModeChanged(ChangeEventArgs e)
        {
            TreeSortMode = NormalizeTreeSortMode(e.Value?.ToString());

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
                ProjectTreeSortMode.Status => ProjectTreeSortMode.Status,
                _ => ProjectTreeSortMode.NameAscending
            };
    }
}
