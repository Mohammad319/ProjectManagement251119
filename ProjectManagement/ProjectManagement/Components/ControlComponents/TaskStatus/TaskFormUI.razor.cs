using Application.Feature.Calculation.TaskStatus.Commands;
using Domain.Entities.Calculation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Components.ControlComponents.TaskStatus;

public partial class TaskFormUI
{
    [Parameter] public TaskStatusEntity Status { get; set; } = new();
    [Parameter] public EventCallback<bool> Callback { get; set; }

    private PostTaskStatusDTO PostStatus { get; set; } = new();
    private bool IsLoading;
    private int? LastStatusId;
    private TaskStatusEntity? LastStatusReference;

    protected override void OnParametersSet()
    {
        var currentId = Status?.Id;
        var sameReference = ReferenceEquals(LastStatusReference, Status);
        if (sameReference && LastStatusId == currentId)
            return;

        PostStatus = new PostTaskStatusDTO();
        PropertyCopier.CopyPropertiesTo(Status, PostStatus);

        LastStatusId = currentId;
        LastStatusReference = Status;
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
                result = await Dispatcher.Send(new CreateTaskStatusCommand(PostStatus)) > 0;
            else
                result = await Dispatcher.Send(new UpdateTaskStatusCommand(Status.Id, PostStatus));

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
