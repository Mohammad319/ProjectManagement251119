using Application.Feature.Project.ProcurementMethods.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Components.ControlComponents.Project.ProcurementMethods
{
    public partial class PMFormUI
    {
        [Parameter] public ProcurementMethodsEntity Procurement { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }
        PostProcurementMethodsDTO ProcurementUpdate { get; set; } = new PostProcurementMethodsDTO();
        bool IsLoading = false;
        protected override void OnInitialized()
        {
            PropertyCopier.CopyPropertiesTo(Procurement, ProcurementUpdate);
        }
        private async Task HandleSubmitAsync()
        {
            IsLoading = true;
            bool result;
            if (Procurement.Id == 0)
                result = await MicroBus.Send(new CreateProcurementMethodsCommand(ProcurementUpdate)) > 0;
            else result = await MicroBus.Send(new UpdateProcurementMethodsCommand(ProcurementUpdate,Procurement.Id));
            MHD.Notifications(Procurement.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
    }
}
