using Application.Feature.Project.Contract.Commands;
using Application.Feature.Project.Contract.Queries;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Contract;

public partial class IndexUI
{
    private bool IsVisible = true;
    private bool IsLoading;
    private List<ContractEntity>? Contracts;

    private List<ContractEntity> VisibleItems =>
        (Contracts ?? []).Where(x => x.IsVisible == IsVisible).OrderBy(x => x.SortOrder).ToList();

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
            Contracts = await Dispatcher.Send(new GetContractQuery());
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task CreateNewAsync()
    {
        UpdateForm(new ContractEntity());
        return Task.CompletedTask;
    }

    private Task ToggleVisibleAsync()
    {
        IsVisible = !IsVisible;
        return InvokeAsync(StateHasChanged);
    }

    private void UpdateForm(ContractEntity model) =>
        MHD.Modal.ShowComponent<ContractFormUI>(
            model.Id == 0 ? "Ny entreprenadform" : AppLoc[LocalizerConst.Update, model.Name],
            new Dictionary<string, object>
            {
                [nameof(ContractFormUI.Contract)] = model,
                [nameof(ContractFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdateAsync)
            });

    private async Task MoveItemAsync(ContractEntity item, bool moveUp)
    {
        var result = await Dispatcher.Send(new MoveContractCommand(item.Id, moveUp));
        if (result)
            await LoadAsync();
    }

    private void Remove(ContractEntity item) =>
        MHD.DeleteMessage(item.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(item)));

    private async Task ConfirmRemoveAsync(ContractEntity item)
    {
        var result = await Dispatcher.Send(new DeleteContractCommand(item.Id));

        if (result)
        {
            Contracts?.Remove(item);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }

    private async Task BtnUpdateAsync(bool isSuccess)
    {
        await MHD.Modal.CloseAsync();

        if (isSuccess)
            await LoadAsync();
    }
}
