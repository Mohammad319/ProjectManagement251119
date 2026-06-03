using Application.Feature.Project.ProjectStatus.Commands;
using Application.Feature.Project.ProjectStatus.Queries;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.ProjectStatus;

public partial class IndexUI
{
    private List<ProjectStatusEntity>? Statuses;
    private bool IsVisible = true;
    private bool IsLoading;

    private List<ProjectStatusEntity> VisibleItems =>
        (Statuses ?? []).Where(x => x.IsVisible == IsVisible).OrderBy(x => x.SortOrder).ToList();

    protected override async Task OnInitializedAsync()
    {
        await LoadStatusesAsync();
    }

    private async Task LoadStatusesAsync()
    {
        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            Statuses = await Dispatcher.Send(new GetProjectStatusQuery());
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task CreateNewAsync()
    {
        UpdateForm(new ProjectStatusEntity());
        return Task.CompletedTask;
    }

    private Task ToggleVisibleAsync()
    {
        IsVisible = !IsVisible;
        return InvokeAsync(StateHasChanged);
    }

    private void UpdateForm(ProjectStatusEntity model) =>
        MHD.Modal.ShowComponent<ProjectStatusFormUI>(
            model.Id == 0 ? "Ny projektstatus" : AppLoc[LocalizerConst.Update, model.Name],
            new Dictionary<string, object>
            {
                [nameof(ProjectStatusFormUI.Status)] = model,
                [nameof(ProjectStatusFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdateAsync)
            });

    private async Task MoveItemAsync(ProjectStatusEntity status, bool moveUp)
    {
        var result = await Dispatcher.Send(new MoveProjectStatusCommand(status.Id, moveUp));
        if (result)
            await LoadStatusesAsync();
    }

    private void Remove(ProjectStatusEntity status)
    {
        MHD.DeleteMessage(status.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(status)));
    }

    private async Task ConfirmRemoveAsync(ProjectStatusEntity st)
    {
        var count = await Dispatcher.Send(new CountProjectsUsingProjectStatusQuery(st.Id));
        if (count > 0)
        {
            MHD.Notifications(ToastType.Delete, false);
            return;
        }

        var result = await Dispatcher.Send(new DeleteProjectStatusCommand(st.Id));

        if (result)
        {
            Statuses?.Remove(st);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }

    private async Task BtnUpdateAsync(bool isSuccess)
    {
        MHD.Modal.Close();

        if (isSuccess)
            await LoadStatusesAsync();
    }
}
