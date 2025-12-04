using Application.Feature.Calculation.StatusResource.Commands;
using Application.Feature.Project.StatusResource.Commands;
using Domain.Entities.Calculation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Components.ControlComponents.ResourceStatus
{
    public partial class ResourceFormUI
    {
        [Parameter] public StatusResourcesEntity ResStatus { get; set; } = new();
        PostResourceStatusDTO PostStatus { get; set; } = new PostResourceStatusDTO();
        [Parameter] public EventCallback<bool> Callback { get; set; }
        bool IsLoading = false;
        protected override void OnInitialized()
        {
            PropertyCopier.CopyPropertiesTo(ResStatus, PostStatus);
        }
        private async Task HandleSubmitAsync()
        {
            IsLoading = true;
            bool result = false;
            if (ResStatus.Id == 0)
                result = await MicroBus.Send(new CreateStatusResourceCommand(PostStatus)) > 0;
            else result = await MicroBus.Send(new UpdateStatusResourceCommand(ResStatus.Id, PostStatus));

            MHD.Notifications(ResStatus.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
    }
}
