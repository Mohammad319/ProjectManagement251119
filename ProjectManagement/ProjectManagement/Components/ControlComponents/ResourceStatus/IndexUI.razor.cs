using Application.Feature.Calculation.StatusResource.Commands;
using Application.Feature.Calculation.StatusResource.Queries;
using Domain.Entities.Calculation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.ResourceStatus;

public partial class IndexUI
{
    private bool IsVisible = true;
    private bool IsLoading;
    private List<StatusResourcesEntity>? Status;

    private IEnumerable<StatusResourcesEntity> VisibleStatuses =>
        (Status ?? [])
            .Where(x => x.IsVisible == IsVisible)
            .OrderByDescending(x => x.SortOrder);

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
            Status = await Dispatcher.Send(new GetResourceStatusQuery());
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task CreateNewAsync() => OpenFormAsync(new StatusResourcesEntity("new", "#00ff00", 0, true));

    private Task ToggleVisibleAsync()
    {
        IsVisible = !IsVisible;
        return InvokeAsync(StateHasChanged);
    }

    private Task OpenFormAsync(StatusResourcesEntity model)
    {
        var title = model.Id == 0
            ? AppLoc[LocalizerConst.New, CalcResource.resourcesStatus]
            : AppLoc[LocalizerConst.Update, model.Name];

        MHD.Modal.ShowComponent<ResourceFormUI>(
            title,
            new Dictionary<string, object>
            {
                [nameof(ResourceFormUI.ResStatus)] = model,
                [nameof(ResourceFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, CallbackAsync)
            });

        return Task.CompletedTask;
    }

    private void Remove(StatusResourcesEntity status)
    {
        MHD.DeleteMessage(status.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(status)));
    }

    private async Task ConfirmRemoveAsync(StatusResourcesEntity st)
    {
        var result = await Dispatcher.Send(new DeleteResourceStatusCommand(st.Id));

        if (result)
        {
            Status?.Remove(st);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }

    private async Task CallbackAsync(bool isSuccess)
    {
        MHD.Modal.Close();

        if (isSuccess)
            await LoadStatusesAsync();
    }
}
