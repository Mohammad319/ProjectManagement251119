using Application.Feature.Project.ProcurementProcedure.Commands;
using Application.Feature.Project.ProcurementProcedure.Queries;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.ProcurementProcedure;

public partial class IndexUI
{
    private bool IsVisible = true;
    private bool IsLoading;
    private List<ProcurementProcedureEntity>? Procedures;

    private List<ProcurementProcedureEntity> VisibleItems =>
        (Procedures ?? []).Where(x => x.IsVisible == IsVisible).OrderBy(x => x.SortOrder).ToList();

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
            Procedures = await Dispatcher.Send(new GetProcurementProcedureQuery());
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task CreateNewAsync()
    {
        UpdateForm(new ProcurementProcedureEntity());
        return Task.CompletedTask;
    }

    private Task ToggleVisibleAsync()
    {
        IsVisible = !IsVisible;
        return InvokeAsync(StateHasChanged);
    }

    private void UpdateForm(ProcurementProcedureEntity model) =>
        MHD.Modal.ShowComponent<ProcurementProcedureFormUI>(
            model.Id == 0 ? "Nytt upphandlingsförfarande" : AppLoc[LocalizerConst.Update, model.Name],
            new Dictionary<string, object>
            {
                [nameof(ProcurementProcedureFormUI.Procedure)] = model,
                [nameof(ProcurementProcedureFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdateAsync)
            });

    private async Task MoveItemAsync(ProcurementProcedureEntity item, bool moveUp)
    {
        var result = await Dispatcher.Send(new MoveProcurementProcedureCommand(item.Id, moveUp));
        if (result)
            await LoadAsync();
    }

    private void Remove(ProcurementProcedureEntity item) =>
        MHD.DeleteMessage(item.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(item)));

    private async Task ConfirmRemoveAsync(ProcurementProcedureEntity item)
    {
        var result = await Dispatcher.Send(new DeleteProcurementProcedureCommand(item.Id));

        if (result)
        {
            Procedures?.Remove(item);
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
