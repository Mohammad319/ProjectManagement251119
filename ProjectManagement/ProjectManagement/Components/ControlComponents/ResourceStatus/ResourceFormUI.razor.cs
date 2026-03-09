using Application.Feature.Calculation.StatusResource.Commands;
using Domain.Entities.Calculation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Components.ControlComponents.ResourceStatus;

public partial class ResourceFormUI
{
    [Parameter] public StatusResourcesEntity ResStatus { get; set; } = new("new status", "#00ff00", 0, true);
    [Parameter] public EventCallback<bool> Callback { get; set; }

    private PostTaskStatusDTO PostStatus { get; set; } = new();
    private bool IsLoading;
    private int? LastStatusId;
    private bool IsInitialized;

    protected override void OnParametersSet()
    {
        var currentId = ResStatus?.Id;
        if (IsInitialized && LastStatusId == currentId)
            return;

        PostStatus = new PostTaskStatusDTO();
        PropertyCopier.CopyPropertiesTo(ResStatus, PostStatus);

        LastStatusId = currentId;
        IsInitialized = true;
    }

    private void CloseModal() => MHD.Modal.Close();

    private async Task HandleSubmitAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            bool result;

            if (ResStatus.Id == 0)
                result = await Dispatcher.Send(new CreateResourceStatusCommand(PostStatus)) > 0;
            else
                result = await Dispatcher.Send(new UpdateResourceStatusCommand(ResStatus.Id, PostStatus));

            MHD.Notifications(ResStatus.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
