using Application.Feature.Project.Status.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Components.ControlComponents.Project.Status
{
    public partial class StatusFormUI
    {
        [Parameter] public StatusEntity Status { get; set; } = new();
        PostTaskStatusDTO StatusUpdate { get; set; } = new();
        [Parameter]
        public EventCallback<bool> Callback { get; set; }
        bool IsLoading = false;
        protected override void OnInitialized()
        {
            PropertyCopier.CopyPropertiesTo(Status, StatusUpdate);
        }
        private async Task HandleSubmitAsync()
        {
            IsLoading = true;
            bool result;
            if (Status.Id == 0)
                result = await MicroBus.Send(new CreateStatusCommand(StatusUpdate)) > 0;
            else result = await MicroBus.Send(new UpdateStatusCommand(Status.Id,StatusUpdate));

            MHD.Notifications(Status.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
    }
}
