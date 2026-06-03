using BlazorMHD.UI.Core.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Client.Shared.Components;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles.Identity;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.App;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Project;
using System.Security.Claims;

namespace ProjectManagement.Client.Pages.Project.ProjectPages
{
    public partial class ProjectForm : AppComponentBase
    {
        public const string DialogFormId = "projectForm";
        [Parameter] public EventCallback<Tuple<bool, ListProjectMVVM>> Callback { get; set; }
        [Parameter] public required ListProjectMVVM Project { get; set; }
        [Parameter] public Guid FolderId { get; set; }

        bool IsLoading = true;

        PostProjectDTO ProjectUpdate = new();
        GetProjectCalcConfigDTO? Config;
        private EditContext? editContext;
        private AddressDTO MainAddress { get; set; } = new();
        private List<ListDTO> ResponsibilityUsers { get; set; } = [];
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        private string ProjectResponsible
        {
            get
            {
                EnsureResponsibleSlot();
                return ProjectUpdate.Responsibles[0];
            }
            set
            {
                EnsureResponsibleSlot();
                ProjectUpdate.Responsibles[0] = value ?? string.Empty;
            }
        }

        private IEnumerable<string> ResponsibilityUserOptions
        {
            get
            {
                var options = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var user in ResponsibilityUsers)
                    AddOption(options, user.Name);

                AddOption(options, ProjectResponsible);
                AddOption(options, ProjectUpdate.Inspector);

                return options.OrderBy(x => x);
            }
        }

        protected override async Task OnInitializedAsync()
        {
            ProjectUpdate.FolderId = FolderId;
            ResetEditContext();

            if (Project.Id != Guid.Empty)
            {
                ProjectUpdate = await Repo.Project.GetToPostAsync(Project.Id)?? new PostProjectDTO();
            }

            ProjectUpdate.Notes ??= [];
            if (ProjectUpdate.Notes.Count == 0)
                ProjectUpdate.Notes.Add(string.Empty);
            ProjectUpdate.Responsibles ??= [];
            EnsureResponsibleSlot();
            ProjectUpdate.Contacts ??= [];
            ProjectUpdate.Address ??= [];
            MainAddress = EnsureMainAddress();
            await ApplyNewProjectResponsibleDefaultAsync();
            await LoadResponsibilityUsersAsync();

            Config = await
                Repo.Project.GetConfig(
                    ProjectUpdate.ProcurementMethodsId,
                    ProjectUpdate.ContractId,
                    ProjectUpdate.CompensationId,
                    ProjectUpdate.TypeId,
                    ProjectUpdate.StatusId,
                    ProjectUpdate.ProcurementProcedureId);

            if (Project.Id == Guid.Empty && !ProjectUpdate.StatusId.HasValue)
                ProjectUpdate.StatusId = Config?.ProjectStatuses?.FirstOrDefault(x => x.IsDefault)?.Id;

            if (Project.Id == Guid.Empty && !ProjectUpdate.TypeId.HasValue)
                ProjectUpdate.TypeId = Config?.Types?.FirstOrDefault(x => x.IsDefault)?.Id;

            if (Project.Id == Guid.Empty && !ProjectUpdate.ContractId.HasValue)
                ProjectUpdate.ContractId = Config?.Contracts?.FirstOrDefault(x => x.IsDefault)?.Id;

            if (Project.Id == Guid.Empty && !ProjectUpdate.CompensationId.HasValue)
                ProjectUpdate.CompensationId = Config?.Compensations?.FirstOrDefault(x => x.IsDefault)?.Id;

            if (Project.Id == Guid.Empty && !ProjectUpdate.ProcurementMethodsId.HasValue)
                ProjectUpdate.ProcurementMethodsId = Config?.Methods?.FirstOrDefault(x => x.IsDefault)?.Id;

            if (Project.Id == Guid.Empty && !ProjectUpdate.ProcurementProcedureId.HasValue)
                ProjectUpdate.ProcurementProcedureId = Config?.Procedures?.FirstOrDefault(x => x.IsDefault)?.Id;

            ResetEditContext();
            IsLoading = false;
        }

        private void ResetEditContext()
        {
            editContext = new EditContext(ProjectUpdate);
            editContext.SetFieldCssClassProvider(RequiredFieldCssClassProvider.Instance);
        }

        private async Task HandleSubmitAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            SyncProjectStatusName();
            NormalizeResponsibilityFields();

            PostProjectDTO entity = new();
            PropertyCopier.CopyPropertiesTo(ProjectUpdate, entity);

            var resultInfo = Tuple.Create(ProjectUpdate.IsVisible, new ListProjectMVVM());
            PropertyCopier.CopyPropertiesTo(ProjectUpdate, resultInfo.Item2);
            resultInfo.Item2.Status = ProjectUpdate.StatusName;
            var selectedStatus = Config?.ProjectStatuses?.FirstOrDefault(x => x.Id == ProjectUpdate.StatusId);
            resultInfo.Item2.CountsAsSubmittedBid = selectedStatus?.CountsAsSubmittedBid ?? false;
            resultInfo.Item2.CountsAsWonBid = selectedStatus?.CountsAsWonBid ?? false;
            resultInfo.Item2.CountsAsLostBid = selectedStatus?.CountsAsLostBid ?? false;
            resultInfo.Item2.Responsible = ProjectUpdate.Responsibles.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

            bool result;

            if (Project.Id != Guid.Empty)
                result = await
                    Repo.Project.UpdateAsync(Project.Id, entity);
            else
            {
                resultInfo.Item2.Id = await Repo.Project.CreateAsync(entity);

                result = resultInfo.Item2.Id != Guid.Empty;
            }

            if (result)
                await Callback.InvokeAsync(resultInfo);

            MHD.Notifications(Project.Id != Guid.Empty ? ToastType.Update : ToastType.Add, result);

            IsLoading = false;
        }

        private AddressDTO EnsureMainAddress()
        {
            ProjectUpdate.Address ??= [];

            if (ProjectUpdate.Address.Count == 0)
                ProjectUpdate.Address.Add(new AddressDTO());

            return ProjectUpdate.Address[0];
        }

        private void SyncProjectStatusName()
        {
            ProjectUpdate.StatusName = ProjectUpdate.StatusId.HasValue
                ? Config?.ProjectStatuses?.FirstOrDefault(x => x.Id == ProjectUpdate.StatusId.Value)?.Name ?? string.Empty
                : string.Empty;
        }

        private void AddNote() => ProjectUpdate.Notes.Add(string.Empty);

        private void RemoveNote(int idx)
        {
            if (idx >= 0 && idx < ProjectUpdate.Notes.Count && ProjectUpdate.Notes.Count > 1)
                ProjectUpdate.Notes.RemoveAt(idx);
        }

        private void EnsureResponsibleSlot()
        {
            ProjectUpdate.Responsibles ??= [];

            while (ProjectUpdate.Responsibles.Count == 0)
                ProjectUpdate.Responsibles.Add(string.Empty);
        }

        private async Task ApplyNewProjectResponsibleDefaultAsync()
        {
            if (Project.Id != Guid.Empty || !string.IsNullOrWhiteSpace(ProjectResponsible))
                return;

            ProjectResponsible = await GetCurrentUserDisplayNameAsync();
        }

        private async Task LoadResponsibilityUsersAsync()
        {
            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (!PMRolesConst.Tenant.AdminManger.Split(',').Any(user.IsInRole))
                    return;

                var departments = Folder.State.Departments?.Any() == true
                    ? Folder.State.Departments
                    : await Repo.Departments.GetDepartmentsAsListAsync() ?? [];

                var usersByName = new Dictionary<string, ListDTO>(StringComparer.OrdinalIgnoreCase);

                foreach (var department in departments)
                {
                    var users = await Repo.Departments.GetUsersAsListAsync(department.Id) ?? [];

                    foreach (var departmentUser in users.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
                        usersByName.TryAdd(departmentUser.Name.Trim(), departmentUser);
                }

                ResponsibilityUsers = [.. usersByName.Values.OrderBy(x => x.Name)];
            }
            catch
            {
                await ClientLog.WarnAsync("Loading project responsibility users failed");
            }
        }

        private async Task<string> GetCurrentUserDisplayNameAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            var fullName = user.FindFirst(PMClaimsConst.FullName)?.Value;
            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;

            return user.Identity?.Name
                ?? user.FindFirst(ClaimTypes.Email)?.Value
                ?? string.Empty;
        }

        private void NormalizeResponsibilityFields()
        {
            EnsureResponsibleSlot();
            ProjectResponsible = ProjectResponsible.Trim();
            ProjectUpdate.Inspector = ProjectUpdate.Inspector?.Trim() ?? string.Empty;
        }

        private static void AddOption(HashSet<string> options, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                options.Add(value.Trim());
        }

    }
}
