using Application.Feature.Project.Compensation.Queries;
using Application.Feature.Project.ProcurementMethods.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Compensation
{
    public partial class IndexUI
    {
        private bool IsVisible = true;
        private bool IsLoading = true;
        private List<CompensationEntity> CompensationList = [];

        private List<CompensationEntity> FilteredCompensationList =>
            CompensationList
                .Where(x => x.IsVisible == IsVisible)
                .OrderByDescending(x => x.SortOrder)
                .ToList();

        private void CreateForm() => UpdateForm(new CompensationEntity());

        private void ToggleVisibleFilter() => IsVisible = !IsVisible;

        private void UpdateForm(CompensationEntity model) =>
            MHD.Modal.ShowComponent<CompensationFormUI>(
                model.Id == 0
                    ? AppLoc[LocalizerConst.New, CalcResource.projectCompensation]
                    : AppLoc[LocalizerConst.Update, model.Name],
                new Dictionary<string, object>
                {
                    [nameof(CompensationFormUI.Compensation)] = model,
                    [nameof(CompensationFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdate)
                });

        private void Remove(CompensationEntity model) =>
            MHD.DeleteMessage(model.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(model)));

        private async Task ConfirmRemoveAsync(CompensationEntity model)
        {
            bool result = await MicroBus.Send(new DeleteCompensationCommand(model.Id));
            if (result)
            {
                CompensationList.RemoveAll(x => x.Id == model.Id);
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
                CompensationList = await MicroBus.Send(new GetCompensationQuery()) ?? [];
            }
            finally
            {
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
