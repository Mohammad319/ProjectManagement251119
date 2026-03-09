using Application.Feature.Project.ProcurementMethods.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Compensation
{
    public partial class CompensationFormUI
    {
        [Parameter] public CompensationEntity Compensation { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }

        private PostTaskStatusDTO CompensationUpdate { get; set; } = new();
        private bool IsLoading;
        private int? LastCompensationId;
        private bool IsInitialized;

        protected override void OnParametersSet()
        {
            var currentId = Compensation?.Id;
            if (IsInitialized && LastCompensationId == currentId)
                return;

            CompensationUpdate = new PostTaskStatusDTO();
            PropertyCopier.CopyPropertiesTo(Compensation, CompensationUpdate);

            if (string.IsNullOrWhiteSpace(CompensationUpdate.Color))
                CompensationUpdate.Color = "#00ff00";

            LastCompensationId = currentId;
            IsInitialized = true;
        }

        private async Task HandleSubmitAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                bool result = Compensation.Id == 0
                    ? await MicroBus.Send(new CreateCompensationCommand(CompensationUpdate)) > 0
                    : await MicroBus.Send(new UpdateCompensationCommand(Compensation.Id, CompensationUpdate));

                MHD.Notifications(Compensation.Id == 0 ? ToastType.Add : ToastType.Update, result);

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
