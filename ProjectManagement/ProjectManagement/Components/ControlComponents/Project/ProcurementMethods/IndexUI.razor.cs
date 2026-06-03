using Application.Feature.Project.ProcurementMethods.Commands;
using Application.Feature.Project.ProcurementMethods.Queries;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.ProcurementMethods;

public partial class IndexUI
{
    private bool IsVisible = true;
    private bool IsLoading;
    private List<ProcurementMethodEntity>? ProcurementMethods;

    private List<ProcurementMethodEntity> VisibleItems =>
        (ProcurementMethods ?? []).Where(x => x.IsVisible == IsVisible).OrderBy(x => x.SortOrder).ToList();

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            ProcurementMethods = await Dispatcher.Send(new GetProcurementMethodsQuery());
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task CreateNewAsync()
    {
        UpdateForm(new ProcurementMethodEntity());
        return Task.CompletedTask;
    }

    private Task ToggleVisibleAsync()
    {
        IsVisible = !IsVisible;
        return InvokeAsync(StateHasChanged);
    }

    private void UpdateForm(ProcurementMethodEntity model) =>
        MHD.Modal.ShowComponent<PMFormUI>(
            model.Id == 0 ? "Ny upphandlingsform" : AppLoc[LocalizerConst.Update, model.Name],
            new Dictionary<string, object>
            {
                [nameof(PMFormUI.Procurement)] = model,
                [nameof(PMFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdateAsync)
            });

    private async Task MoveItemAsync(ProcurementMethodEntity item, bool moveUp)
    {
        var result = await Dispatcher.Send(new MoveProcurementMethodCommand(item.Id, moveUp));
        if (result)
            await LoadAsync();
    }

    private void Remove(ProcurementMethodEntity item) =>
        MHD.DeleteMessage(item.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(item)));

    private async Task ConfirmRemoveAsync(ProcurementMethodEntity item)
    {
        var result = await Dispatcher.Send(new DeleteProcurementMethodCommand(item.Id));

        if (result)
        {
            ProcurementMethods?.Remove(item);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }

    private async Task BtnUpdateAsync(bool isSuccess)
    {
        MHD.Modal.Close();

        if (isSuccess)
            await LoadAsync();
    }
}
