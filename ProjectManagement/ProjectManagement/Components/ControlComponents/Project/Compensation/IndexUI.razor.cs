using Application.Feature.Project.Compensation.Queries;
using Application.Feature.Project.ProcurementMethods.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Compensation;

public partial class IndexUI
{
    private bool IsVisible = true;
    private bool IsLoading;
    private List<CompensationEntity>? Compensations;

    private List<CompensationEntity> VisibleItems =>
        (Compensations ?? []).Where(x => x.IsVisible == IsVisible).OrderBy(x => x.SortOrder).ToList();

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
            Compensations = await Dispatcher.Send(new GetCompensationQuery());
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task CreateNewAsync()
    {
        UpdateForm(new CompensationEntity());
        return Task.CompletedTask;
    }

    private Task ToggleVisibleAsync()
    {
        IsVisible = !IsVisible;
        return InvokeAsync(StateHasChanged);
    }

    private void UpdateForm(CompensationEntity model) =>
        MHD.Modal.ShowComponent<CompensationFormUI>(
            model.Id == 0 ? "Ny ersättningsform" : AppLoc[LocalizerConst.Update, model.Name],
            new Dictionary<string, object>
            {
                [nameof(CompensationFormUI.Compensation)] = model,
                [nameof(CompensationFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdateAsync)
            });

    private async Task MoveItemAsync(CompensationEntity item, bool moveUp)
    {
        var result = await Dispatcher.Send(new MoveCompensationCommand(item.Id, moveUp));
        if (result)
            await LoadAsync();
    }

    private void Remove(CompensationEntity item) =>
        MHD.DeleteMessage(item.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(item)));

    private async Task ConfirmRemoveAsync(CompensationEntity item)
    {
        var result = await Dispatcher.Send(new DeleteCompensationCommand(item.Id));

        if (result)
        {
            Compensations?.Remove(item);
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
