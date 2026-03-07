using Application.Feature.Calculation.ResourceType.Commands;
using Application.Feature.Calculation.ResourceType.Queries;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.DTO.ResourceType;

namespace ProjectManagement.Components.ControlComponents.ResourceType;

public partial class ResourceTypeUI
{
    private bool IsVisible = true;
    private bool IsLoading;
    private List<ResourceTypeModel>? Items;
    private ResourceTypeModel? Sort;

    private IEnumerable<ResourceTypeModel> VisibleItems =>
        (Items ?? []).Where(x => x.IsVisible == IsVisible).OrderByDescending(x => x.Order);

    protected override async Task OnInitializedAsync()
    {
        await LoadItemsAsync();
    }

    private Task CreateNewAsync()
    {
        UpdateForm(new ResourceTypeModel { IsVisible = true });
        return Task.CompletedTask;
    }

    private void OpenSort(ResourceTypeModel item) => Sort = item;

    private Task CloseSortAsync()
    {
        Sort = null;
        return InvokeAsync(StateHasChanged);
    }

    private void UpdateForm(ResourceTypeModel model) =>
        MHD.Modal.ShowComponent<ResourceTypeFormUI>(
            model.Id == 0 ? AppLoc[LocalizerConst.New, CalcResource.resourceType] : AppLoc[LocalizerConst.Update, model.Name],
            new Dictionary<string, object>
            {
                [nameof(ResourceTypeFormUI.ResourceType)] = model,
                [nameof(ResourceTypeFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdateAsync)
            },
            DialogSize.ExtraLarge);

    private async Task ReverseElements()
    {
        IsVisible = !IsVisible;
        await LoadItemsAsync();
    }

    private void Remove(ResourceTypeModel resourceType)
    {
        MHD.DeleteMessage(resourceType.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(resourceType)));
    }

    private async Task ConfirmRemoveAsync(ResourceTypeModel resourceType)
    {
        var result = await Dispatcher.Send(new DeleteResourceTypeCommand(resourceType.Id));

        if (result)
        {
            Items?.Remove(resourceType);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }

    private async Task BtnUpdateAsync(bool isSuccess)
    {
        if (isSuccess)
            await LoadItemsAsync();

        Sort = null;
        MHD.Modal.Close();
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadItemsAsync()
    {
        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            Items = await Dispatcher.Send(new GetResourceTypesQuery(IsVisible));
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
