using Application.Feature.Calculation.TaskStatus.Commands;
using Application.Feature.Calculation.TaskStatus.Queries;
using Domain.Entities.Calculation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles;

namespace ProjectManagement.Components.ControlComponents.TaskStatus;

public partial class IndexUI
{
    private bool IsVisible = true;
    private bool IsLoading;
    private List<TaskStatusEntity>? Status;

    private IEnumerable<TaskStatusEntity> VisibleStatuses =>
        (Status ?? [])
            .Where(x => x.IsVisible == IsVisible)
            .OrderBy(x => x.SortOrder);

    protected override async Task OnInitializedAsync()
    {
        await LoadStatusesAsync();
    }

    private async Task LoadStatusesAsync()
    {
        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            Status = await Dispatcher.Send(new GetTaskStatusQuery());
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task CreateNewAsync()
    {
        UpdateForm(new TaskStatusEntity("New status", "#00ff00", 0, true));
        return Task.CompletedTask;
    }

    private Task ToggleVisibleAsync()
    {
        IsVisible = !IsVisible;
        return InvokeAsync(StateHasChanged);
    }

    private void UpdateForm(TaskStatusEntity model) =>
        MHD.Modal.ShowComponent<TaskFormUI>(
            model.Id == 0 ? AppLoc[LocalizerConst.New, ResourceLoc.taskStatus] : AppLoc[LocalizerConst.Update, model.Name],
            new Dictionary<string, object>
            {
                [nameof(TaskFormUI.Status)] = model,
                [nameof(TaskFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdateAsync)
            });

    private async Task MoveItemAsync(TaskStatusEntity status, bool moveUp)
    {
        var result = await Dispatcher.Send(new MoveTaskStatusCommand(status.Id, moveUp));
        if (result)
            await LoadStatusesAsync();
    }

    private void Remove(TaskStatusEntity status)
    {
        MHD.DeleteMessage(status.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(status)));
    }

    private async Task ConfirmRemoveAsync(TaskStatusEntity st)
    {
        var result = await Dispatcher.Send(new DeleteTaskStatusCommand(st.Id));

        if (result)
        {
            Status?.Remove(st);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }

    private async Task BtnUpdateAsync(bool isSuccess)
    {
        MHD.Modal.Close();

        if (isSuccess)
            await LoadStatusesAsync();
    }
}
