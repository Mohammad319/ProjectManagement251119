using Application.Feature.Project.Status.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Status;

public partial class StatusFormUI
{
    [Parameter] public StatusEntity Status { get; set; } = new();
    [Parameter] public EventCallback<bool> Callback { get; set; }

    private PostTaskStatusDTO StatusUpdate { get; set; } = new();
    private bool IsLoading;

    private bool IsInitialized;
    private int? LastStatusId;

    protected override void OnParametersSet()
    {
        var currentId = Status?.Id;
        if (IsInitialized && LastStatusId == currentId)
            return;

        StatusUpdate = new PostTaskStatusDTO();
        PropertyCopier.CopyPropertiesTo(Status, StatusUpdate);

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

            if (Status.Id == 0)
                result = await Dispatcher.Send(new CreateStatusCommand(StatusUpdate)) > 0;
            else
                result = await Dispatcher.Send(new UpdateStatusCommand(Status.Id, StatusUpdate));

            MHD.Notifications(Status.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
