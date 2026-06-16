using Application.Feature.Project.Type.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Type;

public partial class TypeFormUI
{
    [Parameter] public TypeEntity Status { get; set; } = new();
    [Parameter] public EventCallback<bool> Callback { get; set; }

    private bool IsLoading;
    private PostTaskStatusDTO UpdateObj { get; set; } = new();


    private bool IsInitialized;
    private int? LastStatusId;

    protected override void OnParametersSet()
    {
        var currentId = Status?.Id;
        if (IsInitialized && LastStatusId == currentId)
            return;

        UpdateObj = new PostTaskStatusDTO();
        PropertyCopier.CopyPropertiesTo(Status, UpdateObj);

        LastStatusId = currentId;
        IsInitialized = true;
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
                result = await Dispatcher.Send(new CreateTypeCommand(UpdateObj)) > 0;
            else
                result = await Dispatcher.Send(new UpdateTypeCommand(Status.Id, UpdateObj));

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
