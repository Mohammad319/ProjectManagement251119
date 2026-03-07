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
    private List<TypeEntity>? Status;

    private IEnumerable<TypeEntity> VisibleItems =>
        (Status ?? []).Where(x => x.IsVisible == IsVisible).OrderByDescending(x => x.SortOrder);

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
            Status = await Dispatcher.Send(new GetTypeQuery());
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
            model.Id == 0 ? AppLoc[LocalizerConst.New, CalcResource.type] : AppLoc[LocalizerConst.Update, model.Name],
            new Dictionary<string, object>
            {
                [nameof(TypeFormUI.Status)] = model,
                [nameof(TypeFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdateAsync)
            });

    private void Remove(TypeEntity status) =>
        MHD.DeleteMessage(status.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(status)));

    private async Task ConfirmRemoveAsync(TypeEntity st)
    {
        var result = await Dispatcher.Send(new DeleteTypeCommand(st.Id));

        if (result)
        {
            Status?.Remove(st);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }

    private async Task BtnUpdateAsync(bool isSuccess)
    {
        MHD.Modal.Close();

        if (isSuccess)
            await LoadTypesAsync();
    }
}
