using BlazorMHD.UI.Core.DesignSystem;
using BlazorMHD.UI.Core.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using ProjectManagement.Client.Shared.Components;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.SharedComponent;
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

        // True when the current user's effective permission is Visare: the form is shown read-only
        // and saving is blocked, so they never edit a field that cannot be persisted.
        private bool IsReadOnly;

        PostProjectDTO ProjectUpdate = new();
        GetProjectCalcConfigDTO? Config;
        private EditContext? editContext;
        private AddressDTO MainAddress { get; set; } = new();
        private List<ListDTO> ResponsibilityUsers { get; set; } = [];
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        // localStorage key for the per-user "don't show again" choice. Project-form only — the
        // calculation form uses its own key so the two notices are independent.
        private const string ReviewerInfoStorageKeyPrefix = "atacost.hideReviewerViewOnlyInfo.project";

        // One-time view-only-reviewer notice state. _hideReviewerInfo is loaded once from localStorage.
        private bool _hideReviewerInfo;
        private bool _showReviewerInfoModal;
        private bool _dontShowReviewerInfoAgain;

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

        // --- Option mappers for the shared MhdFormSelect dropdowns ---------------
        // Content/order is untouched; this only adapts the existing config lists to
        // the dropdown's option shape. Project status carries its status color so
        // the dot matches the project list (shared ProjectStatusColor fallback).

        private List<MhdSelectItem<int?>> StatusItems =>
            Config?.ProjectStatuses?
                .Select(s => new MhdSelectItem<int?>
                {
                    Value = s.Id,
                    Label = s.Name,
                    Color = string.IsNullOrWhiteSpace(s.Color) ? ProjectStatusColor.NeutralFallback : s.Color
                })
                .ToList() ?? [];

        private List<MhdSelectItem<string>> UserItems =>
            ResponsibilityUserOptions
                .Select(u => new MhdSelectItem<string> { Value = u, Label = u })
                .ToList();

        // Display names of users whose system role is Visare (view-only). Used to (a) keep them out of
        // the Kalkylansvarig picker (a responsible must be able to change), and (b) show a discreet note
        // when such a person is picked as Granskare (reviewer). Detection is best-effort by display name;
        // when a name can't be resolved we simply don't restrict/notice (graceful degradation).
        private readonly HashSet<string> _viewerUserNames = new(StringComparer.OrdinalIgnoreCase);

        private bool IsViewerName(string? name) =>
            !string.IsNullOrWhiteSpace(name) && _viewerUserNames.Contains(name.Trim());

        // The selected reviewer (Granskare) only has view access → can read and comment, not change.
        private bool SelectedReviewerIsViewerOnly => IsViewerName(ProjectUpdate.Inspector);

        // Kalkylansvarig options exclude view-only users (rule unchanged: responsible must be able to
        // change). The currently selected responsible is always kept so legacy values aren't dropped.
        private List<MhdSelectItem<string>> ResponsibleUserItems =>
            ResponsibilityUserOptions
                .Where(u => !IsViewerName(u) || string.Equals(u, ProjectResponsible, StringComparison.OrdinalIgnoreCase))
                .Select(u => new MhdSelectItem<string> { Value = u, Label = u })
                .ToList();

        private static List<MhdSelectItem<int?>> ToItems(IEnumerable<ListOrderDTO>? source) =>
            source?.Select(x => new MhdSelectItem<int?> { Value = x.Id, Label = x.Name }).ToList() ?? [];

        private static List<MhdSelectItem<int?>> ToItems(IEnumerable<ListDTO>? source) =>
            source?.Select(x => new MhdSelectItem<int?> { Value = x.Id, Label = x.Name }).ToList() ?? [];

        protected override async Task OnInitializedAsync()
        {
            ProjectUpdate.FolderId = FolderId;
            ResetEditContext();

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            bool systemViewer = authState.User.IsInRole(PMRolesConst.Tenant.Viewer);

            if (Project.Id != Guid.Empty)
            {
                ProjectUpdate = await Repo.Project.GetToPostAsync(Project.Id)?? new PostProjectDTO();
            }

            // Read-only for a system Visare or anyone whose effective permission on this project
            // is only Visare (backend-computed CanEdit), so we never let them edit unsavable fields.
            IsReadOnly = systemViewer || !ProjectUpdate.CanEdit;

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

            // Visare can read but not change grunddata — bail with a clear message instead of the
            // generic save error.
            if (IsReadOnly)
            {
                MHD.MessageOk("Behörighet", "Du har visningsbehörighet och kan inte ändra projektets grunddata.", MhdState.Warning);
                return;
            }

            IsLoading = true;
            SyncProjectStatusName();
            NormalizeResponsibilityFields();

            PostProjectDTO entity = new();
            PropertyCopier.CopyPropertiesTo(ProjectUpdate, entity);

            var resultInfo = Tuple.Create(!ProjectUpdate.IsArchived, new ListProjectMVVM());
            PropertyCopier.CopyPropertiesTo(ProjectUpdate, resultInfo.Item2);
            resultInfo.Item2.Status = ProjectUpdate.StatusName;
            var selectedStatus = Config?.ProjectStatuses?.FirstOrDefault(x => x.Id == ProjectUpdate.StatusId);
            resultInfo.Item2.CountsAsSubmittedBid = selectedStatus?.CountsAsSubmittedBid ?? false;
            resultInfo.Item2.CountsAsWonBid = selectedStatus?.CountsAsWonBid ?? false;
            resultInfo.Item2.CountsAsLostBid = selectedStatus?.CountsAsLostBid ?? false;
            resultInfo.Item2.Responsible = ProjectUpdate.Responsibles.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
            resultInfo.Item2.Organisation = Config?.Organisation?.FirstOrDefault(x => x.Id == ProjectUpdate.OrganisationId)?.Name ?? string.Empty;
            resultInfo.Item2.ProcurementMethods = Config?.Methods?.FirstOrDefault(x => x.Id == ProjectUpdate.ProcurementMethodsId)?.Name ?? string.Empty;
            resultInfo.Item2.Contract = Config?.Contracts?.FirstOrDefault(x => x.Id == ProjectUpdate.ContractId)?.Name ?? string.Empty;
            resultInfo.Item2.Compensation = Config?.Compensations?.FirstOrDefault(x => x.Id == ProjectUpdate.CompensationId)?.Name ?? string.Empty;
            resultInfo.Item2.Type = Config?.Types?.FirstOrDefault(x => x.Id == ProjectUpdate.TypeId)?.Name ?? string.Empty;
            resultInfo.Item2.ProcurementProcedure = Config?.Procedures?.FirstOrDefault(x => x.Id == ProjectUpdate.ProcurementProcedureId)?.Name ?? string.Empty;
            resultInfo.Item2.AddressText = FormatAddress(ProjectUpdate.Address.FirstOrDefault());

            try
            {
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
            }
            catch (Exception)
            {
                // The API handlers already surface a global error dialog (e.g. a 403 when the target
                // folder belongs to another department). Swallow here so the exception doesn't tear
                // down the circuit/show the error page — the dialog stays open for a retry.
            }
            finally
            {
                IsLoading = false;
            }
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

                var departments = Folder.State.Departments?.Count > 0
                    ? Folder.State.Departments
                    : await Repo.Departments.GetDepartmentsAsListAsync() ?? [];

                var usersByName = new Dictionary<string, ListDTO>(StringComparer.OrdinalIgnoreCase);
                _viewerUserNames.Clear();

                foreach (var department in departments)
                {
                    var users = await Repo.Departments.GetUsersAuthAsListAsync(department.Id) ?? [];

                    foreach (var departmentUser in users.Where(x => !string.IsNullOrWhiteSpace(x.FullName)))
                    {
                        var name = departmentUser.FullName.Trim();
                        usersByName.TryAdd(name, new ListDTO { Id = departmentUser.UserId ?? 0, Name = name });

                        if (string.Equals(departmentUser.Role, PMRolesConst.Tenant.Viewer, StringComparison.OrdinalIgnoreCase))
                            _viewerUserNames.Add(name);
                    }
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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
                return;

            try
            {
                var key = await GetReviewerInfoStorageKeyAsync();
                var stored = await JS.InvokeAsync<string?>("localStorage.getItem", key);
                _hideReviewerInfo = string.Equals(stored, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
            {
                // JS unavailable (prerender/circuit gone): default to showing the notice.
            }
        }

        // Granskare changed: keep the value, then show the one-time notice when the picked reviewer is
        // view-only and the user hasn't opted out. Selecting a view-only reviewer never grants edit
        // rights — this is purely informational.
        private Task OnInspectorChanged(string? value)
        {
            ProjectUpdate.Inspector = value ?? string.Empty;

            if (SelectedReviewerIsViewerOnly && !_hideReviewerInfo)
            {
                _dontShowReviewerInfoAgain = false;
                _showReviewerInfoModal = true;
            }

            return Task.CompletedTask;
        }

        private async Task DismissReviewerInfoAsync()
        {
            _showReviewerInfoModal = false;

            if (!_dontShowReviewerInfoAgain)
                return;

            _hideReviewerInfo = true;
            try
            {
                var key = await GetReviewerInfoStorageKeyAsync();
                await JS.InvokeVoidAsync("localStorage.setItem", key, "true");
            }
            catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
            {
                // Persisting the preference failed (JS unavailable): the notice simply shows again next time.
            }
        }

        private async Task<string> GetReviewerInfoStorageKeyAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userId = user.FindFirst(PMClaimsConst.UserId)?.Value
                         ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? "anonymous";
            return $"{ReviewerInfoStorageKeyPrefix}.{userId}";
        }

        private static void AddOption(HashSet<string> options, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                options.Add(value.Trim());
        }

        private static string FormatAddress(AddressDTO? address)
        {
            if (address is null)
                return string.Empty;

            var parts = new[]
            {
                address.Street,
                address.Nr,
                address.ZIPCode,
                address.City,
                address.Region,
                address.Country
            };

            return string.Join(", ", parts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        }

    }
}
