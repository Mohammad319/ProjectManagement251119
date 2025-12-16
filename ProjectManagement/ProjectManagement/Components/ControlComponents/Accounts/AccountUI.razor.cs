using Application.Feature.Account.Commands;
using Application.Feature.Account.Queries;
using BlazorMHD.UI.Core.Services;
using ContextMenuMHD;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.DTO.Account;

namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountUI
{
    [Parameter] public int GroupSelected { get; set; }

    private bool IsVisibleOnly { get; set; } = true;
    private bool IsLoading { get; set; }

    private List<ListAccountDTO>? Accounts { get; set; }

    [Inject] private ICommandDispatcher MicroBus { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private ContextMenuService ContextService { get; set; } = default!;
    [Inject] private MhdServices MHD { get; set; } = default!;
    [Inject] private IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;

    private int _lastGroupSelected;

    protected override async Task OnParametersSetAsync()
    {
        if (GroupSelected <= 0) return;

        if (_lastGroupSelected != GroupSelected)
        {
            _lastGroupSelected = GroupSelected;
            await LoadAccountsAsync();
        }
    }

    private async Task LoadAccountsAsync()
    {
        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            // إذا عندك Query يدعم فلترة visible أضفه، وإلا فلتر بالواجهة
            var result = await MicroBus.Send(new GetAccountQuery(GroupSelected));
            Accounts = IsVisibleOnly
                ? result?//.Where(x => x.IsVisible)
                .ToList()
                : result;
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

    private void CreateForm()
    {
        var model = new PostAccountDTO
        {
            AccountGroupId = GroupSelected,
            IsVisible = true,
            Data = new AccountData()
        };

        DialogService.ShowComponent<AccountsFormUI>(
            AppLoc[LocalizerConst.New, CalcResource.account],
            new Dictionary<string, object>
            {
                [nameof(AccountsFormUI.Id)] = 0,
                [nameof(AccountsFormUI.Model)] = model,
                [nameof(AccountsFormUI.OnSaved)] = EventCallback.Factory.Create<bool>(this, OnSavedAsync)
            });
    }

    private void EditForm(ListAccountDTO item)
    {
        var model = new PostAccountDTO
        {
            Account = item.Account,
            Name = item.Name,
            AccountGroupId = GroupSelected,
           // IsVisible = item.IsVisible,
         //   Data = item.Metadata ?? new AccountData()
        };

        DialogService.ShowComponent<AccountsFormUI>(
            AppLoc[LocalizerConst.Update, item.Name],
            new Dictionary<string, object>
            {
                [nameof(AccountsFormUI.Id)] = item.Id,
                [nameof(AccountsFormUI.Model)] = model,
                [nameof(AccountsFormUI.OnSaved)] = EventCallback.Factory.Create<bool>(this, OnSavedAsync)
            });
    }

    private async Task OnSavedAsync(bool ok)
    {
        if (!ok) return;
        await LoadAccountsAsync();
    }

    private async Task AccountContextM(ListAccountDTO item)
    {
        await ContextService.ShowMenuAsync(new()
        {
            new MenuItem
            {
                Label = $"✏️ {ResourceApp.update}",
                OnClickAsync = () =>
                {
                    EditForm(item);
                    return Task.CompletedTask;
                }
            },
            new MenuItem
            {
                Label = $"🗑️ {ResourceApp.delete}",
                OnClickAsync = () =>
                {
                    Remove(item);
                    return Task.CompletedTask;
                }
            }
        });
    }

    private void Remove(ListAccountDTO account) =>
        MHD.DeleteMessage(
            account.Name,
            EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(account)));

    private async Task ConfirmRemoveAsync(ListAccountDTO account)
    {
        var result = await MicroBus.Send(new DeleteAccountCommand(account.Id));
        if (result)
        {
            Accounts?.Remove(account);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }
}
