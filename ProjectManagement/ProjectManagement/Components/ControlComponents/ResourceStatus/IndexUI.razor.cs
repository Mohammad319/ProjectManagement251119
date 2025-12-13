using Application.Feature.Calculation.StatusResource.Commands;
using Application.Feature.Project.StatusResource.Queries;
using Domain.Entities.Calculation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.ResourceStatus
{
    public partial class IndexUI
    {
        bool IsVisible = true;
        List<StatusResourcesEntity>? Status;

        void Remove(StatusResourcesEntity status)
        {
            MHD.DeleteMessage(status.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(status)));
        }

        void ModalForm(StatusResourcesEntity model)
        {
            string title = model.Id == 0 ? AppLoc[LocalizerConst.New, CalcResource.resourcesStatus] : AppLoc[LocalizerConst.Update, model.Name];
            MHD.Modal.ShowComponent<ResourceFormUI>(title,
                new Dictionary<string, object> { [nameof(ResourceFormUI.ResStatus)] = model, [nameof(ResourceFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, Callback) });
        }
        async Task ConfirmRemoveAsync(StatusResourcesEntity st)
        {
            bool result = await MicroBus.Send(new DeleteResourceStatusCommand(st.Id));
            if (result)
            {
                Status?.Remove(st);
                StateHasChanged();
            }
            MHD.Notifications(ToastType.Delete, result);
        }

        async Task Callback(bool IsSuccess)
        {
            MHD.Modal.Close();
            if (IsSuccess)
            {
                Status = await MicroBus.Send(new GetResourceStatusQuery());
            }
        }

        protected async override Task OnInitializedAsync()
        {
            Status = await MicroBus.Send(new GetResourceStatusQuery());
        }
    }
}
