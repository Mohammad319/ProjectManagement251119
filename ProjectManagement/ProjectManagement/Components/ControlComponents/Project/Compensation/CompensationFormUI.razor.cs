using Application.Feature.Project.ProcurementMethods.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Compensation
{
    public partial class CompensationFormUI
    {
        [Parameter] public CompensationEntity Compensation { get; set; } = new("new ", "#00ff00", 0, true);
        [Parameter] public EventCallback<bool> Callback { get; set; }

        PostTaskStatusDTO CompensationUpdate { get; set; } = new();
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
            else result = await MicroBus.Send(new UpdateCompensationCommand(Compensation.Id, CompensationUpdate));

            MHD.Notifications(Compensation.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
    }
}
