using Application.Feature.Application.Commands;
using Application.Feature.Application.Queries;
using Application.Feature.Identity.Department.Queries;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.Base.Application;
using ProjectManagement.Shared.DTO.App;
using ProjectManagement.Shared.DTO.General;

namespace ProjectManagement.Components.ControlComponents.ApplicationTemplate
{
    public partial class ApplicationIndexUI
    {
        private bool IsVisible = true;
        private bool Loading = true;
        private bool _isOpeningCreateDialog;
        private string? _createDialogError;
        private List<ApplicationDTO> Applications = [];
        private List<ListDTO> Departments = [];
        private ApplicationDTO? ApplicationForm;

        private string DepartmentName(ApplicationDTO application)
        {
            if (application.Data.AllDepartments)
                return "Alla avdelningar";

            return Departments.FirstOrDefault(x => x.Id == application.DepartmentId)?.Name ?? "—";
        }

        [Inject] private ILogger<ApplicationIndexUI> Logger { get; set; } = default!;

        private List<ApplicationDTO> VisibleApplications
            => Applications.Where(x => x.IsVisible == IsVisible).ToList();

        private void ToggleVisibility() => IsVisible = !IsVisible;

        private void OpenApplication(ApplicationDTO application)
        {
            if (application.Data.IsSystemTemplate)
            {
                MHD.Notifications(ToastType.Update, false);
                return;
            }

            ApplicationForm = application;
        }

        private void PreviewApplication(ApplicationDTO application)
        {
            MHD.Modal.ShowComponent<ApplicationPreviewUI>(
                $"Förhandsgranskning – {application.Name}",
                new Dictionary<string, object> { [nameof(ApplicationPreviewUI.Application)] = application },
                MhdDialogSize.ExtraLarge);
        }

        private async Task NewAppAsync()
        {
            if (_isOpeningCreateDialog)
                return;

            _isOpeningCreateDialog = true;
            _createDialogError = null;
            await InvokeAsync(StateHasChanged);

            try
            {
                var departmentId = Applications.FirstOrDefault(x => x.DepartmentId > 0)?.DepartmentId ?? 0;
                var template = SelfInspectionStandardTemplates.CreateChecklist(departmentId);
                PrepareNewApplicationTemplate(template);
                ApplicationForm = template;
            }
            catch (Exception ex)
            {
                const string message = "Det gick inte att öppna formuläret för ny egenkontroll. Försök igen eller kontakta administratör.";
                Logger.LogError(ex, "Failed to open create self-inspection template dialog.");
                _createDialogError = message;
                MHD.MessageOk("Fel", message);
            }
            finally
            {
                _isOpeningCreateDialog = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private static void PrepareNewApplicationTemplate(ApplicationDTO template)
        {
            template.Id = 0;
            template.Name = "Ny egenkontrollmall";
            template.IsVisible = true;
            template.LastUpdate = DateTime.Now;
            template.Data ??= new ApplicationDataDTO();
            template.Data.Rows ??= [];
            template.Data.Sections ??= [];
            template.Data.IsSystemTemplate = false;
            template.Data.SystemTemplateKey = string.Empty;
            template.Data.CopiedFromSystemTemplateKey = string.Empty;
            template.Data.TemplateType = string.IsNullOrWhiteSpace(template.Data.TemplateType)
                ? SelfInspectionTemplateTypes.Checklist
                : template.Data.TemplateType;
            template.Data.LinkType = string.IsNullOrWhiteSpace(template.Data.LinkType)
                ? "Kalkyl"
                : template.Data.LinkType;
            template.Data.Purpose = template.Data.Purpose ?? string.Empty;
            template.Data.Description = template.Data.Description ?? template.Data.Purpose;
        }

        private void Remove(ApplicationDTO application)
        {
            if (application.Data.IsSystemTemplate)
            {
                MHD.Notifications(ToastType.Delete, false);
                return;
            }

            MHD.DeleteMessage(application.Name, EventCallback.Factory.Create(this, () => RemoveAsync(application)));
        }

        private async Task CopyApplicationAsync(ApplicationDTO application)
        {
            var copy = SelfInspectionStandardTemplates.CreateCopy(application);
            var result = await Dispatcher.Send(new CreateApplicationCommand(copy)) > 0;
            MHD.Notifications(ToastType.Add, result);
            if (result)
                await GetApplicationsAsync();
        }

        private async Task RemoveAsync(ApplicationDTO application)
        {
            var result = await Dispatcher.Send(new DeleteApplicationCommand(application.Id));
            if (result)
            {
                Applications.RemoveAll(x => x.Id == application.Id);
                await InvokeAsync(StateHasChanged);
            }

            MHD.Notifications(ToastType.Delete, result);
        }

        private async Task BtnUpdateAsync(bool isSuccess)
        {
            if (!isSuccess)
            {
                ApplicationForm = null;
                await InvokeAsync(StateHasChanged);
                return;
            }

            // Ensure the freshly saved (visible) template matches the active filter — if the user was
            // viewing hidden templates, switch back so the new one is guaranteed to show in the list.
            IsVisible = true;
            await GetApplicationsAsync();
            ApplicationForm = null;
            await InvokeAsync(StateHasChanged);
        }

        private async Task GetApplicationsAsync()
        {
            Loading = true;
            Applications = await Dispatcher.Send(new GetApplicationQuery(true)) ?? [];
            Loading = false;
        }

        protected override async Task OnInitializedAsync()
        {
            Departments = await Dispatcher.Send(new GetDepartmentsAsListQuery()) ?? [];
            await GetApplicationsAsync();
        }

        private static int SectionCount(ApplicationDTO application)
        {
            // Prefer the explicit section list; fall back to distinct legacy row descriptions for templates
            // saved before sections became first-class.
            if (application.Data.Sections is { Count: > 0 })
                return application.Data.Sections.Count;

            return application.Data.Rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Description))
                .Select(x => x.Description)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }
    }
}
