using Application.Feature.Project.Status.Commands;
using Application.Feature.Project.Status.Queries;
using Application.Feature.Project.Status.Queries.Application.Feature.Calculation.Status.Queries;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Status
{
    public partial class IndexUI
    {
        List<StatusEntity>? Status;
        void UpdateForm(StatusEntity model) =>
    MHD.Modal.ShowComponent<StatusFormUI>(model.Id == 0 ? AppLoc[LocalizerConst.New, CalcResource.projectContract] :
                AppLoc[LocalizerConst.Update, model.Name],
new Dictionary<string, object> { [nameof(StatusFormUI.Status)] = model, [nameof(StatusFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdate) });

        bool IsVisible = true;
        void Remove(StatusEntity status)
        {
            MHD.DeleteMessage(status.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(status)));
        }
        async Task ConfirmRemoveAsync(StatusEntity st)
        {
            bool result = await MicroBus.Send(new DeleteStatusCommand(st.Id));
            if (result)
            {
                Status?.Remove(st);
                StateHasChanged();
            }
            MHD.Notifications(ToastType.Delete, result);
        }
        async Task BtnUpdate(bool IsSuccess)
        {
            MHD.Modal.Close();
            if (IsSuccess)
            {
                Status = await MicroBus.Send(new GetStatusQuery());
            }
        }

        protected async override Task OnInitializedAsync()
        {
            Status = await MicroBus.Send(new GetStatusQuery());
        }
    }
}
