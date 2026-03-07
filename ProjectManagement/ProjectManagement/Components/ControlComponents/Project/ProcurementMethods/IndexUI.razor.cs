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
        private bool IsVisible = true;
        private bool IsLoading = true;
        private List<ProcurementMethodEntity> ProcurementMethods = [];

        private List<ProcurementMethodEntity> FilteredProcurementMethods =>
            ProcurementMethods
                .Where(x => x.IsVisible == IsVisible)
                .OrderByDescending(x => x.SortOrder)
                .ToList();

        private void CreateForm() => UpdateForm(new ProcurementMethodEntity());

        private void ToggleVisibleFilter() => IsVisible = !IsVisible;

        private void UpdateForm(ProcurementMethodEntity model) =>
            MHD.Modal.ShowComponent<PMFormUI>(
                model.Id == 0
                    ? AppLoc[LocalizerConst.New, CalcResource.procurementMethods]
                    : AppLoc[LocalizerConst.Update, model.Name],
                new Dictionary<string, object>
                {
                    [nameof(PMFormUI.Procurement)] = model,
                    [nameof(PMFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdate)
                });

        private void Remove(ProcurementMethodEntity item) =>
            MHD.DeleteMessage(item.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(item)));

        private async Task ConfirmRemoveAsync(ProcurementMethodEntity item)
        {
            bool result = await MicroBus.Send(new DeleteProcurementMethodCommand(item.Id));
            if (result)
            {
                ProcurementMethods.RemoveAll(x => x.Id == item.Id);
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
                ProcurementMethods = await MicroBus.Send(new GetProcurementMethodsQuery()) ?? [];
            }
            finally
            {
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
