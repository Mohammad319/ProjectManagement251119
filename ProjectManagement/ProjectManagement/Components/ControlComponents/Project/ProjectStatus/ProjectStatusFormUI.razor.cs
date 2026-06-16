using Application.Feature.Project.ProjectStatus.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.ProjectStatus;

public partial class ProjectStatusFormUI
{
    [Parameter] public ProjectStatusEntity Status { get; set; } = new();
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

    private void OnCountsAsSubmittedChanged(ChangeEventArgs e)
    {
        StatusUpdate.CountsAsSubmittedBid = e.Value is bool b && b;
        if (!StatusUpdate.CountsAsSubmittedBid)
        {
            StatusUpdate.CountsAsWonBid = false;
            StatusUpdate.CountsAsLostBid = false;
        }
    }

    private void OnCountsAsWonChanged(ChangeEventArgs e)
    {
        StatusUpdate.CountsAsWonBid = e.Value is bool b && b;
        if (StatusUpdate.CountsAsWonBid)
        {
            StatusUpdate.CountsAsSubmittedBid = true;
            StatusUpdate.CountsAsLostBid = false;
        }
    }

    private void OnCountsAsLostChanged(ChangeEventArgs e)
    {
        StatusUpdate.CountsAsLostBid = e.Value is bool b && b;
        if (StatusUpdate.CountsAsLostBid)
        {
            StatusUpdate.CountsAsSubmittedBid = true;
            StatusUpdate.CountsAsWonBid = false;
        }
    }

    private void CloseModal() => MHD.Modal.CloseAsync();

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
                result = await Dispatcher.Send(new CreateProjectStatusCommand(StatusUpdate)) > 0;
            else
                result = await Dispatcher.Send(new UpdateProjectStatusCommand(Status.Id, StatusUpdate));

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
