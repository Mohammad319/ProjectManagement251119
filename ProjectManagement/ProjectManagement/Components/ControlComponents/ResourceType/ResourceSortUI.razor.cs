using Application.Feature.Calculation.ResourceType.Commands;
using Application.Feature.Calculation.ResourceType.Queries;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.DTO.ResourceType;

namespace ProjectManagement.Components.ControlComponents.ResourceType
{
    public partial class ResourceSortUI
    {
        [Parameter] public EventCallback Callback { get; set; }
        [Parameter] public int ResourceTypeId { get; set; }
        List<ResourceSortModel> ResourceSorts = [];
        async Task GetSortResourcesAsync()
        {
            ResourceSorts = await MicroBus.Send(new GetResourceSortQuery(ResourceTypeId));
        }

        protected async override Task OnInitializedAsync()
        {
            //MHD.Loading.New();
            await GetSortResourcesAsync();
            //MHD.Loading.Close();
        }

        void Remove(ResourceSortModel resourceType)
        {
            MHD.DeleteMessage(resourceType.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(resourceType)));
        }

        async Task ConfirmRemoveAsync(ResourceSortModel resourceType)
        {
            bool result = await MicroBus.Send(new DeleteResourceSortCommand(resourceType.Id));
            if (result)
            {
                ResourceSorts.Remove(resourceType);
                StateHasChanged();
            }
            MHD.Notifications(ToastType.Delete, result);
        }
        void UpdateForm(ResourceSortModel model)
        {
            model.ResourceTypeId = ResourceTypeId;
            MHD.Modal.ShowComponent<ResourceSortFormUI>(model.Id == 0 ? AppLoc[LocalizerConst.New, CalcResource.resourceType] :
                AppLoc[LocalizerConst.Update, model.Name],
                new Dictionary<string, object> { [nameof(ResourceSortFormUI.ResourceSort)] = model, [nameof(ResourceSortFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, GetSortResourcesAsync) }, DialogSize.ExtraLarge);
        }
    }
}
