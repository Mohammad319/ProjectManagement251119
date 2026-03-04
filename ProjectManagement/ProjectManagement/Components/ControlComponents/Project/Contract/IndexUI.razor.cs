using Application.Feature.Project.Contract.Commands;
using Application.Feature.Project.Contract.Queries;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.Model.Project;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Contract
{
    public partial class IndexUI
    {
        bool IsVisible = true;

        List<ContractEntity>? ContractList;
        void UpdateForm(ContractEntity model) =>
    MHD.Modal.ShowComponent<ContractFormUI>(model.Id == 0 ? AppLoc[LocalizerConst.New, CalcResource.projectContract] :
                AppLoc[LocalizerConst.Update, model.Name],
new Dictionary<string, object> { [nameof(ContractFormUI.Contract)] = model, [nameof(ContractFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdate) });

        void Remove(ContractEntity model)
        {
            MHD.DeleteMessage(model.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(model)));
        }
        async Task ConfirmRemoveAsync(ContractEntity model)
        {
            bool result = await MicroBus.Send(new DeleteContractCommand(model.Id));
            if (result)
            {
                ContractList?.Remove(model);
            }
            MHD.Notifications(ToastType.Delete, result);

            StateHasChanged();
        }
        async Task BtnUpdate(bool IsSuccess)
        {
            MHD.Modal.Close();
            if (IsSuccess)
            {
                ContractList = await MicroBus.Send(new GetContractQuery());
            }
            StateHasChanged();
        }

        protected async override Task OnInitializedAsync()
        {
            ContractList = await MicroBus.Send(new GetContractQuery());
        }
    }
}
