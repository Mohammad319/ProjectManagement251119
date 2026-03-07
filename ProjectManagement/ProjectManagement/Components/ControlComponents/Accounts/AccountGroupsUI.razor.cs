using Application.Feature.Account.Commands;
using Application.Feature.Account.Queries;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.General;

namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountGroupsUI
{
    private int? GroupSelected;
    private List<ListDTO>? Groups;
    private bool IsLoading;

    protected override async Task OnInitializedAsync()
    {
        await RefreshGroupsAsync();
    }

    private async Task<List<ListDTO>> LoadGroupsAsync() =>
        await Dispatcher.Send(new GetAccountGroupsQuery());

    private async Task RefreshGroupsAsync()
    {
        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            Groups = await LoadGroupsAsync();

            if (GroupSelected.HasValue && Groups?.Any(x => x.Id == GroupSelected.Value) == true)
                return;

            GroupSelected = Groups?.FirstOrDefault()?.Id;
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ChangeAccountGroupSelectedAsync(ListDTO item)
    {
        if (GroupSelected == item.Id)
            return;

        GroupSelected = item.Id;
        await InvokeAsync(StateHasChanged);
    }

    private void ImportForm() =>
        DialogService.ShowComponent<AccountImportFromFile>(
            AppLoc[LocalizerConst.Import, CalcResource.accountGroups],
            new Dictionary<string, object>
            {
                [nameof(AccountImportFromFile.OnSaved)] =
                    EventCallback.Factory.Create<bool>(this, OnImportSavedAsync)
            },
            DialogSize.ExtraLarge);

    private void CreateForm()
    {
        DialogService.ShowComponent<AccountGroupsFormUI>(
            AppLoc[LocalizerConst.New, CalcResource.accountGroups],
            new Dictionary<string, object>
            {
                [nameof(AccountGroupsFormUI.Id)] = 0,
                [nameof(AccountGroupsFormUI.Model)] = new PostAccountGroupDTO(),
                [nameof(AccountGroupsFormUI.OnSaved)] =
                    EventCallback.Factory.Create<bool>(this, RefreshAsync)
            });
    }

    private void EditForm(ListDTO item)
    {
        DialogService.ShowComponent<AccountGroupsFormUI>(
            AppLoc[LocalizerConst.Update, item.Name],
            new Dictionary<string, object>
            {
                [nameof(AccountGroupsFormUI.Id)] = item.Id,
                [nameof(AccountGroupsFormUI.Model)] =
                    new PostAccountGroupDTO { Name = item.Name },
                [nameof(AccountGroupsFormUI.OnSaved)] =
                    EventCallback.Factory.Create<bool>(this, RefreshAsync)
            });
    }

    private async Task OnImportSavedAsync(bool refresh)
    {
        if (!refresh)
            return;

        await RefreshGroupsAsync();
    }

    private async Task RefreshAsync(bool refresh)
    {
        if (!refresh)
            return;

        await RefreshGroupsAsync();
    }

    private void Remove(ListDTO item) =>
        MHD.DeleteMessage(
            item.Name,
            EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(item)));

    private async Task ConfirmRemoveAsync(ListDTO item)
    {
        var result = await Dispatcher.Send(new DeleteAccountGroupCommand(item.Id));

        if (result)
        {
            Groups?.Remove(item);

            if (GroupSelected == item.Id)
                GroupSelected = Groups?.FirstOrDefault()?.Id;

            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }

    private async Task GroupContextM(ListDTO item)
    {
        await ContextService.ShowMenuAsync(
        [
            new MenuItem
            {
                Label = $"✏️ {AppLoc[nameof(ResourceApp.update)]}",
                OnClickAsync = () =>
                {
                    EditForm(item);
                    return Task.CompletedTask;
                }
            },
            new MenuItem
            {
                Label = $"🗑️ {AppLoc[nameof(ResourceApp.delete)]}",
                OnClickAsync = () =>
                {
                    Remove(item);
                    return Task.CompletedTask;
                }
            }
        ]);

        GroupSelected = item.Id;
        await InvokeAsync(StateHasChanged);
    }
}
