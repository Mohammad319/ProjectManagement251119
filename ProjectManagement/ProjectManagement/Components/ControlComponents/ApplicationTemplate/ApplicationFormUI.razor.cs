using Application.Feature.Application.Commands;
using Application.Feature.Identity.Department.Queries;
using BlazorMHD.UI.Core.DesignSystem;
using Domain.Entities.Application;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared.DTO.General;

namespace ProjectManagement.Components.ControlComponents.ApplicationTemplate
{
    public partial class ApplicationFormUI
    {
        [Parameter] public ApplicationEntity ApplicationUpdate { get; set; } = new();
        List<ListDTO>? Departments;
        RowEntity RowForm;
        [Parameter] public EventCallback<bool> Callback { get; set; }
        bool IsLoading = false;
        protected async override Task OnInitializedAsync()
        {
            Departments = await MicroBus.Send(new GetDepartmentsAsListQuery());
            ApplicationUpdate.DepartmentId = Departments.FirstOrDefault().Id;
        }
        private async Task HandleSubmitAsync()
        {
            IsLoading = true;
            bool isSuccess = false;
            if (ApplicationUpdate.Id == 0)
                isSuccess = await MicroBus.Send(new CreateApplicationCommand(ApplicationUpdate)) > 0;

            else isSuccess = await MicroBus.Send(new UpdateApplicationCommand(ApplicationUpdate));
            MHD.Notifications(ApplicationUpdate.Id == 0 ? ToastType.Add : ToastType.Update, isSuccess);
            await Callback.InvokeAsync(isSuccess);
        }

        void CallBackRowForm(RowEntity row)
        {
            if (row != null)
            {
                if (row.ID == Guid.Empty)
                {
                    row.ID = Guid.NewGuid();
                    ApplicationUpdate.Data.Rows.Add(row);
                }
                else
                {
                    RowForm.Name = row.Name;
                    RowForm.Description = row.Description;
                    RowForm.StyleRow = row.StyleRow;
                    RowForm.Style = row.Style;
                    RowForm.Attributes = row.Attributes;
                }
            }
            RowForm = null;
        }

        void Remove(RowEntity row)
        {
            MHD.MessageYesNo(ResourceApp.delete,
                AppLoc[LocalizerConst.deleteConfirmMsg, row.Name],
               MhdState.Warning, EventCallback.Factory.Create(this, () => RemoveAsync(row)));
        }
        bool RemoveAsync(RowEntity st)
        {
            ApplicationUpdate.Data.Rows.Remove(st);
            StateHasChanged();
            return true;
        }
    }
}
