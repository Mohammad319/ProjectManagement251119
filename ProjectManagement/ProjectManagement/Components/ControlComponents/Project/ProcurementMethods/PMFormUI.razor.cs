using Application.Feature.Project.ProcurementMethods.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Components.ControlComponents.Project.ProcurementMethods
{
    public partial class PMFormUI
    {
        [Parameter] public ProcurementMethodEntity Procurement { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }

        private PostTaskStatusDTO ProcurementUpdate { get; set; } = new();
        private bool IsLoading;
        private int? LastProcurementId;
        private bool IsInitialized;

        protected override void OnParametersSet()
        {
            var currentId = Procurement?.Id;
            if (IsInitialized && LastProcurementId == currentId)
                return;

            ProcurementUpdate = new PostTaskStatusDTO();
            PropertyCopier.CopyPropertiesTo(Procurement, ProcurementUpdate);

            if (string.IsNullOrWhiteSpace(ProcurementUpdate.Color))
                ProcurementUpdate.Color = "#00ff00";

            LastProcurementId = currentId;
            IsInitialized = true;
        }

        private async Task HandleSubmitAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                bool result = Procurement.Id == 0
                    ? await MicroBus.Send(new CreateProcurementMethodCommand(ProcurementUpdate)) > 0
                    : await MicroBus.Send(new UpdateProcurementMethodCommand(Procurement.Id, ProcurementUpdate));

                MHD.Notifications(Procurement.Id == 0 ? ToastType.Add : ToastType.Update, result);

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
