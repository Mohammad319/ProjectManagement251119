using Application.Feature.Calculation.ResourceType.Commands;
using Application.Feature.Calculation.ResourceType.Queries;
using BlazorMHD.UI.Core.Services;
using DocumentFormat.OpenXml.Vml.Spreadsheet;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.DTO.ResourceType;

namespace ProjectManagement.Components.ControlComponents.ResourceType
{
    public partial class ResourceTypeUI
    {
        bool IsVisible = true;
        List<ResourceTypeModel>? Items;
        ResourceTypeModel? Sort;
        void UpdateForm(ResourceTypeModel model) =>
            MHD.Modal.ShowComponent<ResourceTypeFormUI>(model.Id == 0 ? AppLoc[LocalizerConst.New, CalcResource.resourceType] :
                AppLoc[LocalizerConst.Update, model.Name],
                new Dictionary<string, object> { [nameof(ResourceTypeFormUI.ResourceType)] = model, [nameof(ResourceTypeFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdate) }, DialogSize.ExtraLarge);
        async Task ReverseElements()
        {
            IsVisible = !IsVisible;
            await GetStatus();
        }
        void Remove(ResourceTypeModel resourceType)
        {
            MHD.DeleteMessage(resourceType.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(resourceType)));
        }

        async Task ConfirmRemoveAsync(ResourceTypeModel resourceType)
        {
            bool result = await MicroBus.Send(new DeleteResourceTypeCommand(resourceType.Id));
            if (result)
            {
                Items?.Remove(resourceType);
                StateHasChanged();
            }
            MHD.Notifications(ToastType.Delete, result);
        }

        async Task BtnUpdate(bool IsSuccess)
        {
            if (IsSuccess)
            {
                await GetStatus();
            }
            Sort = null;
            MHD.Modal.Close();
            StateHasChanged();
        }

        async Task GetStatus()
        {
            Items = await MicroBus.Send(new GetResourceTypesQuery(IsVisible));
        }

        protected async override Task OnInitializedAsync()
        {
            await GetStatus();
        }
    }
}
