using BlazorMHD.UI.Core.DesignSystem;
using Application.Feature.Application.Commands;
using Application.Feature.Identity.Department.Queries;
using Domain.Entities.Application;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared.DTO.General;

namespace ProjectManagement.Components.ControlComponents.ApplicationTemplate
{
    public partial class ApplicationFormUI
    {
        [Parameter] public ApplicationEntity ApplicationUpdate { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }

        private List<ListDTO> Departments = [];
        private RowEntity? RowForm;
        private bool IsLoading;

        protected override async Task OnParametersSetAsync()
        {
            Departments = await Dispatcher.Send(new GetDepartmentsAsListQuery()) ?? [];

            ApplicationUpdate.Data ??= new ApplicationDataEntity();
            ApplicationUpdate.Data.Rows ??= [];

            if (ApplicationUpdate.Id == 0 && ApplicationUpdate.DepartmentId <= 0)
            {
                var firstDepartmentId = Departments.FirstOrDefault()?.Id;
                if (firstDepartmentId.HasValue)
                    ApplicationUpdate.DepartmentId = firstDepartmentId.Value;
            }
        }

        private void OpenNewRow() => RowForm = new RowEntity();

        private async Task HandleSubmitAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            var isSuccess = false;

            try
            {
                ApplicationUpdate.Name = (ApplicationUpdate.Name ?? string.Empty).Trim();
                ApplicationUpdate.Data ??= new ApplicationDataEntity();
                ApplicationUpdate.Data.Rows ??= [];

                if (ApplicationUpdate.Id == 0)
                    isSuccess = await Dispatcher.Send(new CreateApplicationCommand(ApplicationUpdate)) > 0;
                else
                    isSuccess = await Dispatcher.Send(new UpdateApplicationCommand(ApplicationUpdate));
            }
            finally
            {
                IsLoading = false;
            }

            MHD.Notifications(ApplicationUpdate.Id == 0 ? ToastType.Add : ToastType.Update, isSuccess);
            await Callback.InvokeAsync(isSuccess);
        }

        private void CallBackRowForm(RowEntity row)
        {
            if (row is not null)
            {
                if (row.ID == Guid.Empty)
                {
                    row.ID = Guid.NewGuid();
                    ApplicationUpdate.Data.Rows.Add(row);
                }
                else if (RowForm is not null)
                {
                    RowForm.Name = row.Name;
                    RowForm.Description = row.Description;
                    RowForm.StyleRow = row.StyleRow;
                    RowForm.Style = row.Style;
                    RowForm.Attributes = row.Attributes;
                    RowForm.IsVisible = row.IsVisible;
                }
            }

            RowForm = null;
        }

        private void Remove(RowEntity row)
        {
            MHD.MessageYesNo(ResourceApp.delete,
                AppLoc[LocalizerConst.deleteConfirmMsg, row.Name],
                MhdState.Warning,
                EventCallback.Factory.Create(this, () => RemoveAsync(row)));
        }

        private bool RemoveAsync(RowEntity row)
        {
            ApplicationUpdate.Data.Rows.Remove(row);
            StateHasChanged();
            return true;
        }

        private Task CancelAsync() => Callback.InvokeAsync(false);
    }
}
