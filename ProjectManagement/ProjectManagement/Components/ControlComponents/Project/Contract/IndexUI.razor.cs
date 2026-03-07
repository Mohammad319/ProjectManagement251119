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
        private bool IsVisible = true;
        private bool IsLoading = true;
        private List<ContractEntity> ContractList = [];

        private List<ContractEntity> FilteredContractList =>
            ContractList
                .Where(x => x.IsVisible == IsVisible)
                .OrderByDescending(x => x.SortOrder)
                .ToList();

        private void CreateForm() => UpdateForm(new ContractEntity());

        private void ToggleVisibleFilter() => IsVisible = !IsVisible;

        private void UpdateForm(ContractEntity model) =>
            MHD.Modal.ShowComponent<ContractFormUI>(
                model.Id == 0
                    ? AppLoc[LocalizerConst.New, CalcResource.projectContract]
                    : AppLoc[LocalizerConst.Update, model.Name],
                new Dictionary<string, object>
                {
                    [nameof(ContractFormUI.Contract)] = model,
                    [nameof(ContractFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdate)
                });

        private void Remove(ContractEntity model) =>
            MHD.DeleteMessage(model.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(model)));

        private async Task ConfirmRemoveAsync(ContractEntity model)
        {
            bool result = await MicroBus.Send(new DeleteContractCommand(model.Id));
            if (result)
            {
                ContractList.RemoveAll(x => x.Id == model.Id);
                await InvokeAsync(StateHasChanged);
            }

            MHD.Notifications(ToastType.Delete, result);
        }

        private async Task BtnUpdate(bool isSuccess)
        {
            if (!isSuccess)
                return;

            MHD.Modal.Close();
            await LoadAsync();
        }

        protected override Task OnInitializedAsync() => LoadAsync();

        private async Task LoadAsync()
        {
            IsLoading = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                ContractList = await MicroBus.Send(new GetContractQuery()) ?? [];
            }
            finally
            {
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
