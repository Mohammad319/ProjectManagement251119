using Application.Feature.Project.Compensation.Commands;
using Application.Feature.Project.ProcurementMethods.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Components.ControlComponents.Project.Compensation
{
    public partial class CompensationFormUI
    {
        [Parameter] public CompensationEntity Compensation { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }

        PostCompensationDTO CompensationUpdate { get; set; } = new();
        bool IsLoading = false;
        protected override void OnInitialized()
        {
            PropertyCopier.CopyPropertiesTo(Compensation, CompensationUpdate);
        }
        private async Task HandleSubmitAsync()
        {
            IsLoading = true;
            bool result = false;
            if (Compensation.Id == 0)
                result = await MicroBus.Send(new CreateCompensationCommand(CompensationUpdate)) > 0;
            else result = await MicroBus.Send(new UpdateCompensationCommand(CompensationUpdate, Compensation.Id));

            MHD.Notifications(Compensation.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
    }
}
