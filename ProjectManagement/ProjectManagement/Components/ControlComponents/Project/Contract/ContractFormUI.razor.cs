using Application.Feature.Project.Contract.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Components.ControlComponents.Project.Contract
{
    public partial class ContractFormUI
    {
        [Parameter] public ContractEntity Contract { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }

        private PostTaskStatusDTO UpdateObj { get; set; } = new();
        private bool IsLoading;

        protected override void OnParametersSet()
        {
            UpdateObj = new PostTaskStatusDTO();
            PropertyCopier.CopyPropertiesTo(Contract, UpdateObj);

            if (string.IsNullOrWhiteSpace(UpdateObj.Color))
                UpdateObj.Color = "#00ff00";
        }

        private async Task HandleSubmitAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                bool result = Contract.Id == 0
                    ? await MicroBus.Send(new CreateContractCommand(UpdateObj)) > 0
                    : await MicroBus.Send(new UpdateContractCommand(Contract.Id, UpdateObj));

                MHD.Notifications(Contract.Id == 0 ? ToastType.Add : ToastType.Update, result);

                if (result && Callback.HasDelegate)
                    await Callback.InvokeAsync(true);
            }
            finally
            {
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private void Close() => MHD.Modal.Close();
    }
}
