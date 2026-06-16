using Application.Feature.Project.Type.Commands;
using Application.Feature.Project.Type.Queries;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Type;

public partial class IndexUI
{
    private bool IsVisible = true;
    private bool IsLoading;
    private List<TypeEntity>? Types;

    private List<TypeEntity> VisibleItems =>
        (Types ?? []).Where(x => x.IsVisible == IsVisible).OrderBy(x => x.SortOrder).ToList();

    protected override async Task OnInitializedAsync()
    {
        await LoadTypesAsync();
    }

    private async Task LoadTypesAsync()
    {
        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            Types = await Dispatcher.Send(new GetTypeQuery());
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task CreateNewAsync()
    {
        UpdateForm(new TypeEntity());
        return Task.CompletedTask;
    }

    private Task ToggleVisibleAsync()
    {
        IsVisible = !IsVisible;
        return InvokeAsync(StateHasChanged);
    }

    private void UpdateForm(TypeEntity model) =>
        MHD.Modal.ShowComponent<TypeFormUI>(
            model.Id == 0 ? "Ny projekttyp" : AppLoc[LocalizerConst.Update, model.Name],
            new Dictionary<string, object>
            {
                [nameof(TypeFormUI.Status)] = model,
                [nameof(TypeFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdateAsync)
            });

    private async Task MoveItemAsync(TypeEntity item, bool moveUp)
    {
        var result = await Dispatcher.Send(new MoveTypeCommand(item.Id, moveUp));
        if (result)
            await LoadTypesAsync();
    }

    private void Remove(TypeEntity item) =>
        MHD.DeleteMessage(item.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(item)));

    private async Task ConfirmRemoveAsync(TypeEntity item)
    {
        var result = await Dispatcher.Send(new DeleteTypeCommand(item.Id));

        if (result)
        {
            Types?.Remove(item);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }

    private async Task BtnUpdateAsync(bool isSuccess)
    {
        await MHD.Modal.CloseAsync();

        if (isSuccess)
            await LoadTypesAsync();
    }
}
