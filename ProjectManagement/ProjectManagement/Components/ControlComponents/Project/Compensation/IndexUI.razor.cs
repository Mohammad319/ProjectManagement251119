using Application.Feature.Project.Compensation.Queries;
using Application.Feature.Project.ProcurementMethods.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Compensation
{
    public partial class IndexUI
    {
        bool IsVisible = true;
        List<CompensationEntity>? CompensationList;
        void UpdateForm(CompensationEntity model) =>
    MHD.Modal.ShowComponent<CompensationFormUI>(model.Id == 0 ? AppLoc[LocalizerConst.New, CalcResource.projectCompensation] :
                AppLoc[LocalizerConst.Update, model.Name], new Dictionary<string, object> { [nameof(CompensationFormUI.Compensation)] = model, [nameof(CompensationFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdate) }
               );
        void Remove(CompensationEntity model) =>
        MHD.DeleteMessage(model.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(model)));

        async Task ConfirmRemoveAsync(CompensationEntity model)
        {
            bool result = await MicroBus.Send(new DeleteCompensationCommand(model.Id));
            if (result)
            {
                CompensationList?.Remove(model);
            }
            MHD.Notifications(ToastType.Delete, result);

            StateHasChanged();
        }
        async Task BtnUpdate(bool IsSuccess)
        {
            MHD.Modal.Close();
            if (IsSuccess)
            {
                CompensationList = await MicroBus.Send(new GetCompensationQuery());
            }
            StateHasChanged();
        }

        protected async override Task OnInitializedAsync()
        {
            CompensationList = await MicroBus.Send(new GetCompensationQuery());
        }
    }
}
