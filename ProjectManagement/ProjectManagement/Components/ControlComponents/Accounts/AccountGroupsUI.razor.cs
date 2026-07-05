using Application.Feature.Account.Commands;
using Application.Feature.Account.Queries;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Components.ControlComponents.Department;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.General;

namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountGroupsUI
{
    private enum VisibleFilter { All, Visible, Hidden }

    private List<AccountManageDTO> AllAccounts = [];
    private List<ListDTO> Groups = [];
    private bool IsLoading = true;

    // Toolbar state
    private string SearchText = string.Empty;
    private int? GroupFilter;
    private VisibleFilter VisibleState = VisibleFilter.All;
    private bool ShowCommentColumn = true;

    // Sorting
    private string SortColumn = "group";
    private bool SortAscending = true;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            Groups = await Dispatcher.Send(new GetAccountGroupsQuery()) ?? [];
            AllAccounts = await Dispatcher.Send(new GetAccountsOverviewQuery()) ?? [];
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private IReadOnlyList<AppSelect<int?>.Option> GroupFilterOptions
    {
        get
        {
            var options = new List<AppSelect<int?>.Option>
            {
                new(null, "Alla kontogrupper")
            };
            options.AddRange(Groups.Select(g => new AppSelect<int?>.Option(g.Id, g.Name)));
            return options;
        }
    }

    private static string FirstComment(AccountManageDTO account)
        => account.Metadata?.Comments?.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? string.Empty;

    private IEnumerable<AccountManageDTO> VisibleRows
    {
        get
        {
            IEnumerable<AccountManageDTO> query = AllAccounts;

            if (GroupFilter.HasValue)
                query = query.Where(x => x.AccountGroupId == GroupFilter.Value);

            query = VisibleState switch
            {
                VisibleFilter.Visible => query.Where(x => x.IsVisible),
                VisibleFilter.Hidden => query.Where(x => !x.IsVisible),
                _ => query
            };

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                query = query.Where(x =>
                    Contains(x.Code, term) ||
                    Contains(x.Name, term) ||
                    Contains(x.AccountGroupName, term) ||
                    Contains(FirstComment(x), term));
            }

            query = (SortColumn, SortAscending) switch
            {
                ("code", true) => query.OrderBy(x => x.Code),
                ("code", false) => query.OrderByDescending(x => x.Code),
                ("name", true) => query.OrderBy(x => x.Name),
                ("name", false) => query.OrderByDescending(x => x.Name),
                ("visible", true) => query.OrderBy(x => x.IsVisible),
                ("visible", false) => query.OrderByDescending(x => x.IsVisible),
                (_, true) => query.OrderBy(x => x.AccountGroupName).ThenBy(x => x.Code),
                (_, false) => query.OrderByDescending(x => x.AccountGroupName).ThenBy(x => x.Code),
            };

            return query;
        }
    }

    private bool HasAnyAccounts => AllAccounts.Count > 0;

    private static bool Contains(string? source, string term)
        => source?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false;

    private void SortBy(string column)
    {
        if (SortColumn == column)
            SortAscending = !SortAscending;
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
    }

    private string SortIndicator(string column)
        => SortColumn != column ? string.Empty : (SortAscending ? "▲" : "▼");

    private void CycleVisibleFilter()
        => VisibleState = VisibleState switch
        {
            VisibleFilter.All => VisibleFilter.Visible,
            VisibleFilter.Visible => VisibleFilter.Hidden,
            _ => VisibleFilter.All
        };

    private string VisibleFilterLabel => VisibleState switch
    {
        VisibleFilter.Visible => AppLoc[nameof(ResourceApp.visible)],
        VisibleFilter.Hidden => AppLoc[nameof(ResourceApp.hiddenItems)],
        _ => "Alla"
    };

    // ---------------------------------------------------------------
    // Account CRUD
    // ---------------------------------------------------------------
    private void CreateAccount()
    {
        if (Groups.Count == 0)
        {
            MHD.Notifications(ToastType.Info, false);
            return;
        }

        OpenAccountForm(0, new PostAccountDTO
        {
            AccountGroupId = GroupFilter ?? Groups.First().Id,
            IsVisible = true,
            Data = new AccountData()
        }, AppLoc[LocalizerConst.New, CalcResource.account]);
    }

    private void EditAccount(AccountManageDTO item) => OpenAccountForm(
        item.Id,
        new PostAccountDTO
        {
            Account = item.Code,
            Name = item.Name,
            AccountGroupId = item.AccountGroupId,
            IsVisible = item.IsVisible,
            Data = item.Data ?? new AccountData(),
        },
        AppLoc[LocalizerConst.Update, item.Name]);

    private void OpenAccountForm(int id, PostAccountDTO model, string title)
    {
        DialogService.ShowComponent<AccountsFormUI>(
            title,
            new Dictionary<string, object>
            {
                [nameof(AccountsFormUI.Id)] = id,
                [nameof(AccountsFormUI.Model)] = model,
                [nameof(AccountsFormUI.Groups)] = Groups,
                [nameof(AccountsFormUI.OnSaved)] = EventCallback.Factory.Create<bool>(this, OnAccountSavedAsync)
            },
            MhdDialogSize.ExtraLarge,
            DialogButtonsHelper.CreateSaveCancelButtons(AccountsFormUI.DialogFormId));
    }

    private async Task OnAccountSavedAsync(bool ok)
    {
        if (ok)
            await LoadAsync();
    }

    private void RemoveAccount(AccountManageDTO account) =>
        MHD.DeleteMessage(account.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAccountAsync(account)));

    private async Task ConfirmRemoveAccountAsync(AccountManageDTO account)
    {
        var result = await Dispatcher.Send(new DeleteAccountCommand(account.Id));
        if (result)
        {
            AllAccounts.RemoveAll(x => x.Id == account.Id);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }

    // ---------------------------------------------------------------
    // Account groups
    // ---------------------------------------------------------------
    private void ManageGroups()
    {
        DialogService.ShowComponent<AccountGroupsManageUI>(
            "Hantera kontogrupper",
            new Dictionary<string, object>
            {
                [nameof(AccountGroupsManageUI.OnChanged)] = EventCallback.Factory.Create(this, LoadAsync)
            },
            MhdDialogSize.Large);
    }

    private void ImportForm() =>
        DialogService.ShowComponent<AccountImportFromFile>(
            AppLoc[LocalizerConst.Import, CalcResource.accountGroups],
            new Dictionary<string, object>
            {
                [nameof(AccountImportFromFile.OnSaved)] = EventCallback.Factory.Create<bool>(this, OnImportSavedAsync)
            },
            MhdDialogSize.ExtraLarge);

    private async Task OnImportSavedAsync(bool refresh)
    {
        if (refresh)
            await LoadAsync();
    }
}
