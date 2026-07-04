using Application.Feature.Application.Commands;
using Application.Feature.Application.Queries;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.Base.Application;
using ProjectManagement.Shared.DTO.App;

namespace ProjectManagement.Components.ControlComponents.ApplicationTemplate
{
    public partial class ApplicationIndexUI
    {
        private bool IsVisible = true;
        private bool Loading = true;
        private List<ApplicationDTO> Applications = [];
        private ApplicationDTO? ApplicationForm;

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

        private void NewApp()
        {
            var departmentId = Applications.FirstOrDefault(x => x.DepartmentId > 0)?.DepartmentId ?? 0;
            ApplicationForm = SelfInspectionStandardTemplates.CreateChecklist(departmentId);
            ApplicationForm.Name = string.Empty;
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
                return;

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
            await GetApplicationsAsync();
        }

        private static int SectionCount(ApplicationDTO application)
            => application.Data.Rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Description))
                .Select(x => x.Description)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
    }
}
