using Application.Feature.Calculation.ResourceType.Commands;
using Application.Feature.Calculation.ResourceType.Queries;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.DTO.ResourceType;

namespace ProjectManagement.Components.ControlComponents.ResourceType;

public partial class ResourceSortUI
{
    [Parameter] public EventCallback Callback { get; set; }
    [Parameter] public int ResourceTypeId { get; set; }

    private List<ResourceSortModel> ResourceSorts = [];
    private bool IsLoading;
    private int _lastResourceTypeId;

    protected override async Task OnParametersSetAsync()
    {
        if (_lastResourceTypeId == ResourceTypeId && ResourceSorts.Count > 0)
            return;

        _lastResourceTypeId = ResourceTypeId;
        await GetSortResourcesAsync();
    }

    private async Task GetSortResourcesAsync()
    {
        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            ResourceSorts = await Dispatcher.Send(new GetResourceSortQuery(ResourceTypeId));
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task CreateNewAsync()
    {
        UpdateForm(new ResourceSortModel { ResourceTypeId = ResourceTypeId, IsVisible = true });
        return Task.CompletedTask;
    }

    private void Remove(ResourceSortModel resourceType)
    {
        MHD.DeleteMessage(resourceType.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(resourceType)));
    }

    private async Task ConfirmRemoveAsync(ResourceSortModel resourceType)
    {
        bool result;

        try
        {
            result = await Dispatcher.Send(new DeleteResourceSortCommand(resourceType.Id));
        }
        catch
        {
            result = false;
        }

        if (result)
        {
            ResourceSorts.Remove(resourceType);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }

    private void UpdateForm(ResourceSortModel model)
    {
        model.ResourceTypeId = ResourceTypeId;

        MHD.Modal.ShowComponent<ResourceSortFormUI>(
            model.Id == 0 ? AppLoc[LocalizerConst.New, CalcResource.resourceType] : AppLoc[LocalizerConst.Update, model.Name],
            new Dictionary<string, object>
            {
                [nameof(ResourceSortFormUI.ResourceSort)] = model,
                [nameof(ResourceSortFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, OnSavedAsync)
            },
            DialogSize.ExtraLarge,
            DialogButtonsHelper.CreateSaveCancelButtons(ResourceSortFormUI.DialogFormId));
    }

    private async Task OnSavedAsync(bool isSuccess)
    {
        if (isSuccess)
            await GetSortResourcesAsync();

        MHD.Modal.Close();
    }
}
