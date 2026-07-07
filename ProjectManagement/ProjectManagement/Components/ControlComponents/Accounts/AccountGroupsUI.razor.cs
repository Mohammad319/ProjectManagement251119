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

    // Column chooser. Standard columns: Kod, Namn, Kontogrupp, Synlig, Kommentar 1, Åtgärder.
    private bool _columnsMenuOpen;
    private bool ShowGroupColumn = true;
    private bool ShowVisibleColumn = true;
    private bool ShowComment1 = true;
    private bool ShowComment2 = false;

    // Sorting
    private string SortColumn = "group";
    private bool SortAscending = true;

    // Pagination
    private int PageSize = 25;
    private int CurrentPage = 1;
    private static readonly int[] PageSizeOptions = [10, 25, 50, 100];

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

    private static string CommentAt(AccountManageDTO account, int index)
    {
        var comments = account.Metadata?.Comments;
        return comments is not null && index < comments.Count ? comments[index] ?? string.Empty : string.Empty;
    }

    private static string Comment1(AccountManageDTO account) => CommentAt(account, 0);
    private static string Comment2(AccountManageDTO account) => CommentAt(account, 1);

    // Filtered + sorted, materialised so pagination and the result counter agree.
    private List<AccountManageDTO> FilteredRows
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
                    Contains(Comment1(x), term) ||
                    Contains(Comment2(x), term));
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

            return query.ToList();
        }
    }

    private int PageCount => Math.Max(1, (int)Math.Ceiling(FilteredRows.Count / (double)PageSize));

    // Clamp the current page to the available range (filter/search may have shrunk the result set).
    private int SafePage => Math.Min(Math.Max(1, CurrentPage), PageCount);

    private IEnumerable<AccountManageDTO> PagedRows
        => FilteredRows.Skip((SafePage - 1) * PageSize).Take(PageSize);

    private int FilteredCount => FilteredRows.Count;

    private bool HasActiveFilters
        => GroupFilter.HasValue
           || VisibleState != VisibleFilter.All
           || !string.IsNullOrWhiteSpace(SearchText);

    private string ResultSummary
        => FilteredCount == 0
            ? "0 träffar"
            : $"Visar {FilteredCount} av {AllAccounts.Count}";

    private void ClearFilters()
    {
        GroupFilter = null;
        VisibleState = VisibleFilter.All;
        SearchText = string.Empty;
        CurrentPage = 1;
    }

    private void PrevPage()
    {
        if (SafePage > 1)
            CurrentPage = SafePage - 1;
    }

    private void NextPage()
    {
        if (SafePage < PageCount)
            CurrentPage = SafePage + 1;
    }

    private void OnPageSizeChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var size) && size > 0)
        {
            PageSize = size;
            CurrentPage = 1;
        }
    }

    private void ToggleColumnsMenu() => _columnsMenuOpen = !_columnsMenuOpen;

    private bool ShowComment1Column => ShowComment1;
    private bool ShowComment2Column => ShowComment2;

    // Kod, Namn, Åtgärder always + the toggleable columns.
    private int ColumnCount => 3
        + (ShowGroupColumn ? 1 : 0)
        + (ShowVisibleColumn ? 1 : 0)
        + (ShowComment1Column ? 1 : 0)
        + (ShowComment2Column ? 1 : 0);

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
        VisibleFilter.Visible => "Synliga",
        VisibleFilter.Hidden => "Dolda",
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
            MHD.ToastInfo("Kontot har tagits bort.", string.Empty, true);
        }
        else
        {
            MHD.Notifications(ToastType.Delete, false);
        }
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
            MhdDialogSize.FullScreen);

    private async Task OnImportSavedAsync(bool refresh)
    {
        if (refresh)
            await LoadAsync();
    }
}
