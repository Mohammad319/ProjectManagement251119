using Application.Feature.Project.Contract.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Components.ControlComponents.Project.Contract
{
    public partial class ContractFormUI
    {
        [Parameter] public ContractEntity Contract { get; set; } = new("new", "#00ff00", 0, true);
        PostTaskStatusDTO UpdateObj { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }
        bool IsLoading = false;
        protected override void OnInitialized()
        {
            PropertyCopier.CopyPropertiesTo(Contract, UpdateObj);
        }
        private async Task HandleSubmitAsync()
        {
            IsLoading = true;

            bool result;
            if (Contract.Id == 0)
                result = await MicroBus.Send(new CreateContractCommand(UpdateObj)) > 0;
            else result = await MicroBus.Send(new UpdateContractCommand(Contract.Id,UpdateObj));

            MHD.Notifications(Contract.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
    }
}
