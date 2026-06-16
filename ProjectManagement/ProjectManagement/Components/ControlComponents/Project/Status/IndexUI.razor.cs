using Application.Feature.Project.Status.Commands;
using Application.Feature.Project.Status.Queries;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Status;

public partial class IndexUI
{
    private List<StatusEntity>? Status;
    private bool IsVisible = true;
    private bool IsLoading;

    private List<StatusEntity> VisibleItems =>
        (Status ?? []).Where(x => x.IsVisible == IsVisible).OrderBy(x => x.SortOrder).ToList();

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
            Status = await Dispatcher.Send(new GetStatusQuery());
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task CreateNewAsync()
    {
        UpdateForm(new StatusEntity());
        return Task.CompletedTask;
    }

    private Task ToggleVisibleAsync()
    {
        IsVisible = !IsVisible;
        return InvokeAsync(StateHasChanged);
    }

    private void UpdateForm(StatusEntity model) =>
        MHD.Modal.ShowComponent<StatusFormUI>(
            model.Id == 0 ? CalcLoc[nameof(CalcResource.newStatus)] : AppLoc[LocalizerConst.Update, model.Name],
            new Dictionary<string, object>
            {
                [nameof(StatusFormUI.Status)] = model,
                [nameof(StatusFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdateAsync)
            });

    private async Task MoveItemAsync(StatusEntity status, bool moveUp)
    {
        var result = await Dispatcher.Send(new MoveStatusCommand(status.Id, moveUp));
        if (result)
            await LoadStatusesAsync();
    }

    private void Remove(StatusEntity status)
    {
        MHD.DeleteMessage(status.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(status)));
    }

    private async Task ConfirmRemoveAsync(StatusEntity st)
    {
        var count = await Dispatcher.Send(new CountCalculationsUsingStatusQuery(st.Id));
        if (count > 0)
        {
            MHD.Notifications(ToastType.Delete, false);
            return;
        }

        var result = await Dispatcher.Send(new DeleteStatusCommand(st.Id));

        if (result)
        {
            Status?.Remove(st);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }

    private async Task BtnUpdateAsync(bool isSuccess)
    {
        await MHD.Modal.CloseAsync();

        if (isSuccess)
            await LoadStatusesAsync();
    }
}
