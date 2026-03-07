using Application.Feature.Account.Commands;
using Application.Feature.Account.Queries;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.DTO.Account;

namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountUI
{
    [Parameter] public int GroupSelected { get; set; }

    private bool IsVisibleOnly = true;
    private bool IsLoading;

    private List<AccountManageDTO>? Accounts;
    private int _lastGroupSelected;

    protected override async Task OnParametersSetAsync()
    {
        if (GroupSelected <= 0)
        {
            Accounts = null;
            _lastGroupSelected = 0;
            return;
        }

        if (_lastGroupSelected != GroupSelected)
        {
            _lastGroupSelected = GroupSelected;
            await LoadAccountsAsync();
        }
    }

    private async Task LoadAccountsAsync()
    {
        if (GroupSelected <= 0)
        {
            Accounts = null;
            return;
        }

        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await Dispatcher.Send(new GetAccountQuery(GroupSelected));
            var all = result?.ToList() ?? [];

            Accounts = IsVisibleOnly
                ? all.Where(x => x.IsVisible).ToList()
                : all;
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ToggleVisibleFilter()
    {
        IsVisibleOnly = !IsVisibleOnly;
        await LoadAccountsAsync();
    }

    private void CreateForm() => OpenForm(0, new PostAccountDTO
    {
        AccountGroupId = GroupSelected,
        IsVisible = true,
        Data = new AccountData()
    }, AppLoc[LocalizerConst.New, CalcResource.account]);

    private void EditForm(AccountManageDTO item) => OpenForm(
        item.Id,
        new PostAccountDTO
        {
            Account = item.Code,
            Name = item.Name,
            AccountGroupId = GroupSelected,
            IsVisible = item.IsVisible,
            Data = item.Metadata ?? new AccountData(),
        },
        AppLoc[LocalizerConst.Update, item.Name]);

    private void OpenForm(int id, PostAccountDTO model, string title)
    {
        DialogService.ShowComponent<AccountsFormUI>(
            title,
            new Dictionary<string, object>
            {
                [nameof(AccountsFormUI.Id)] = id,
                [nameof(AccountsFormUI.Model)] = model,
                [nameof(AccountsFormUI.OnSaved)] =
                    EventCallback.Factory.Create<bool>(this, OnSavedAsync)
            },
            DialogSize.ExtraLarge);
    }

    private async Task OnSavedAsync(bool ok)
    {
        if (!ok)
            return;

        await LoadAccountsAsync();
    }

    private async Task AccountContextM(AccountManageDTO item)
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
    }

    private void Remove(AccountManageDTO account) =>
        MHD.DeleteMessage(
            account.Name,
            EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(account)));

    private async Task ConfirmRemoveAsync(AccountManageDTO account)
    {
        var result = await Dispatcher.Send(new DeleteAccountCommand(account.Id));

        if (result)
        {
            Accounts?.Remove(account);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }
}
