using Application.Feature.Project.ProcurementMethods.Commands;
using Application.Feature.Project.ProcurementMethods.Queries;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.Model.Project;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.ProcurementMethods
{
    public partial class IndexUI
    {
        bool IsVisible = true;
        List<ProcurementMethodEntity>? Status;
        void UpdateForm(ProcurementMethodEntity model) =>
    MHD.Modal.ShowComponent<PMFormUI>(model.Id == 0 ? AppLoc[LocalizerConst.New, CalcResource.procurementMethods] :
                AppLoc[LocalizerConst.Update, model.Name],
new Dictionary<string, object> { [nameof(PMFormUI.Procurement)] = model, [nameof(PMFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdate) });

        void Remove(ProcurementMethodEntity status)
        {
            MHD.DeleteMessage(status.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(status)));
        }
        async Task ConfirmRemoveAsync(ProcurementMethodEntity st)
        {
            bool result = await MicroBus.Send(new DeleteProcurementMethodCommand(st.Id));
            if (result)
            {
                Status?.Remove(st);
            }
            MHD.Notifications(ToastType.Delete, result);

            StateHasChanged();
        }
        async Task BtnUpdate(bool IsSuccess)
        {
            MHD.Modal.Close();
            if (IsSuccess)
            {
                Status = await MicroBus.Send(new GetProcurementMethodsQuery());
            }
            StateHasChanged();
        }

        protected async override Task OnInitializedAsync()
        {
            Status = await MicroBus.Send(new GetProcurementMethodsQuery());
        }
    }
}
