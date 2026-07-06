using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared;
using ProjectManagement.Shared.Constant;
using BlazorMHD.UI.Core.Services;
using ProjectManagement.Client.Helper;
using Domain.DTO.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using ProjectManagement.Components.Account;
using ProjectManagement.Services;
using ProjectManagement.Services.UI;
using Application.Feature.Identity.Department.Queries;
using ProjectManagement.Shared.DTO.General;
using Microsoft.JSInterop;
using ProjectManagement.Client.Shared.ResourceFiles.Identity;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using BlazorMHD.UI.Core.Data;

namespace ProjectManagement.Components.ControlComponents.Department;

public partial class UsersIndex : IAsyncDisposable
{
    private const string FilterStorageKey = "DepartmentUsers.Filters";

    // Proof-of-concept for MhdTable's resize/frozen-column engine. Widths live for the
    // lifetime of this Server circuit (no DB backing here) and are fed back via
    // ColumnWidths; the JS engine also keeps them across re-renders within the session.
    private readonly Dictionary<string, int> _userColumnWidths = new();

    private void OnUserColumnResized(MhdColumnWidthChange change)
        => _userColumnWidths[change.ColumnKey] = change.Width;

    /// <summary>Swedish labels for the MhdTable chrome (pagination, search, sort tooltips).</summary>
    protected Dictionary<string, string> TableLocalization { get; } = new()
    {
        ["Prev"] = "Föregående",
        ["Next"] = "Nästa",
        ["Page"] = "Sida",
        ["Of"] = "av",
        ["RowsPerPage"] = "Rader per sida",
        ["SearchPlaceholder"] = "Sök...",
        ["Export"] = "Exportera",
        ["NoData"] = "Inga rader",
        ["Loading"] = "Laddar",
        ["SortAsc"] = "Sortera stigande",
        ["SortDesc"] = "Sortera fallande",
    };

    [Parameter] public int? DepartmentId { get; set; }
    [Parameter] public bool WithoutDepartmentOnly { get; set; }
    [Parameter] public EventCallback<bool> OnClickCallback { get; set; }

    [Inject] public ITenantUserService TenantUserService { get; set; } = default!;
    [Inject] public IDepartmentUsersViewService DepartmentUsersViewService { get; set; } = default!;
    [Inject] public IStringLocalizer<PMWebResource> WebLoc { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;
    [Inject] public IAccountNotificationEmailSender AccountEmailSender { get; set; } = default!;

    protected List<TenantUserDto>? Users { get; set; }
    protected bool IsLoading { get; set; } = true;
    protected bool IsBusy { get; set; }
    protected string? LoadError { get; set; }
    protected TenantUserDto? DetailsUser { get; set; }
    protected TenantUserDto? ActionMenuUser { get; set; }
    protected double ActionMenuLeftPx { get; set; } = 16;
    protected double ActionMenuTopPx { get; set; } = 96;
    protected string ActionMenuStyle => FormattableString.Invariant(
        $"left: clamp(1rem, {ActionMenuLeftPx}px, calc(100vw - 17rem)); top: clamp(4rem, {ActionMenuTopPx}px, max(4rem, calc(100vh - 28rem))); max-width: calc(100vw - 2rem); max-height: calc(100vh - 5rem);");

    protected const string ActionMenuItemClass =
        "user-action-menu-item flex w-full items-center justify-start gap-2 rounded-md px-2 py-1.5 text-left text-xs text-slate-700 transition hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-60 dark:text-slate-200 dark:hover:bg-slate-800";

    protected const string ActionMenuDangerClass =
        "user-action-menu-item flex w-full items-center justify-start gap-2 rounded-md px-2 py-1.5 text-left text-xs text-rose-600 transition hover:bg-rose-50 disabled:cursor-not-allowed disabled:opacity-60 dark:text-rose-300 dark:hover:bg-rose-950/40";

    private int? _lastDepartmentId;
    private bool _lastWithoutDepartmentOnly;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _searchCts;
    private List<TenantUserDto>? _filteredCache;
    private string _searchTerm = string.Empty;

    // Filtering / search state
    protected string SearchInput { get; set; } = string.Empty;
    protected string SearchTerm
    {
        get => _searchTerm;
        set
        {
            if (_searchTerm == value) return;
            _searchTerm = value;
            InvalidateFilteredUsers();
        }
    }

    // Multi-choice filters. Empty set = no restriction on that dimension.
    protected HashSet<string> RoleFilters { get; } = new(StringComparer.OrdinalIgnoreCase);
    protected HashSet<string> StatusFilters { get; } = new(StringComparer.Ordinal);
    protected HashSet<int> DepartmentFilters { get; } = new();

    // Options carry the DISPLAY label as their value; filtering compares GetRoleName(user).
    // This is deliberate: both the Guest tier and the separate Viewer role render as "Visare",
    // so a single "Visare" option must match users of either underlying role (the old
    // role-value filter only matched TenantGuest and returned nothing for TenantViewer users).
    protected IReadOnlyList<FilterMultiSelect<string>.Option> RoleFilterOptions =>
    [
        new(WebLoc["LevelManager"].Value, WebLoc["LevelManager"].Value),
        new(WebLoc["LevelUser"].Value, WebLoc["LevelUser"].Value),
        new(WebLoc["LevelGuest"].Value, WebLoc["LevelGuest"].Value),
    ];

    // Status filter is deliberately limited to the five states that are distinct and actionable.
    // "Ansluten" (auth) was dropped as a duplicate of Aktiv, "Inaktiv 90+ dagar" (stale) and
    // "Användare utan avdelning" (nodept) were removed — the latter now lives in the department filter.
    protected IReadOnlyList<FilterMultiSelect<string>.Option> StatusFilterOptions =>
    [
        new("active", WebLoc["StatusActive"].Value),
        new("pending", WebLoc["InvitePending"].Value),
        new("locked", "Inloggningsspärr"),
        new("inactive", WebLoc["StatusInactive"].Value),
        new("noauth", WebLoc["UserMissingAuth"].Value),
    ];

    /// <summary>Sentinel option value in the department filter that matches users with no department.</summary>
    internal const int NoDepartmentFilterKey = -1;

    // "Användare utan avdelning" is offered first, followed by the real departments.
    protected IReadOnlyList<FilterMultiSelect<int>.Option> DepartmentFilterOptions =>
    [
        new(NoDepartmentFilterKey, WebLoc["UsersWithoutDepartmentLabel"].Value),
        .. (DepartmentsList ?? [])
            .Select(d => new FilterMultiSelect<int>.Option(d.Id, d.Name ?? "—")),
    ];

    /// <summary>Invoked by the filter dropdowns after they mutate a selection set in place.</summary>
    protected async Task OnFilterChangedAsync()
    {
        InvalidateFilteredUsers();
        await SaveFilterPreferencesAsync();
        await InvokeAsync(StateHasChanged);
    }

    protected List<ListDTO>? DepartmentsList { get; set; }

    /// <summary>Accounts considered inactive when their last sign-in is older than this many days.</summary>
    private const int StaleDays = 90;

    protected int TotalCount => Users?.Count ?? 0;
    protected int FilteredCount => FilteredUsers.Count;
    protected int RegisteredCount => Users?.Count(x => x.IsInAuth) ?? 0;
    protected int LockedCount => FilteredUsers.Count(IsLocked);
    protected int WithoutDepartmentCount => FilteredUsers.Count(x => GetDepartmentIds(x).Count == 0);
    protected int ActiveCount => FilteredUsers.Count(IsActive);
    protected int PendingCount => FilteredUsers.Count(IsPending);
    protected int StaleCount => FilteredUsers.Count(IsStale);
    protected int NoAuthCount => FilteredUsers.Count(x => !x.IsInAuth);
    protected int InactiveCount => FilteredUsers.Count(IsDeactivated);

    /// <summary>The tenant's licensed user limit (0 = unknown/not enforced in the UI).</summary>
    protected int TenantMaxUsers { get; private set; }
    protected bool ShowCapacity => TenantMaxUsers > 0;
    protected bool CapacityReached => ShowCapacity && RegisteredCount >= TenantMaxUsers;
    protected bool CapacityNearLimit => ShowCapacity && !CapacityReached && RegisteredCount >= TenantMaxUsers - 1;

    // Role distribution — counted through GetRoleName so the totals match exactly what the
    // table shows. Note the "Visare" bucket covers BOTH the Guest tier (Tenant.User) and the
    // separate Viewer role (Tenant.Viewer); the old count only looked at Tenant.User and so
    // reported 0 for tenants whose viewers are on the Tenant.Viewer role.
    protected int ManagerCount => FilteredUsers.Count(x => x.Role == PMRolesConst.Tenant.Admin);
    protected int MemberCount => FilteredUsers.Count(x => x.Role == PMRolesConst.Tenant.Manger);
    protected int GuestCount => FilteredUsers.Count(x => x.Role is PMRolesConst.Tenant.User or PMRolesConst.Tenant.Viewer);

    protected bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchTerm)
        || RoleFilters.Count > 0
        || StatusFilters.Count > 0
        || DepartmentFilters.Count > 0;

    protected IReadOnlyList<TenantUserDto> FilteredUsers
        => _filteredCache ??= ComputeFilteredUsers();

    private List<TenantUserDto> ComputeFilteredUsers()
    {
        IEnumerable<TenantUserDto> query = Users ?? Enumerable.Empty<TenantUserDto>();

        if (RoleFilters.Count > 0)
            query = query.Where(u => RoleFilters.Contains(GetRoleName(u)));

        // Department filter (OR within the dimension). The sentinel key matches users with no department.
        if (DepartmentFilters.Count > 0)
        {
            var includeNoDept = DepartmentFilters.Contains(NoDepartmentFilterKey);
            query = query.Where(u =>
                GetDepartmentIds(u).Any(DepartmentFilters.Contains)
                || (includeNoDept && GetDepartmentIds(u).Count == 0));
        }

        // Any selected status matches (OR within the dimension).
        if (StatusFilters.Count > 0)
            query = query.Where(u => StatusFilters.Any(s => MatchesStatus(u, s)));

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim();
            query = query.Where(u =>
                MatchesTerm(u.Email, term)
                || MatchesTerm(u.Username, term)
                || MatchesTerm(u.Firstname, term)
                || MatchesTerm(u.Lastname, term)
                || MatchesTerm(u.PhoneNumber, term)
                || MatchesTerm(GetDepartmentName(u), term));
        }

        return query.ToList();
    }

    private void InvalidateFilteredUsers() => _filteredCache = null;

    private static bool MatchesTerm(string? value, string term)
        => !string.IsNullOrEmpty(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private bool MatchesStatus(TenantUserDto u, string status) => status switch
    {
        "active" => IsActive(u),
        "pending" => IsPending(u),
        "stale" => IsStale(u),
        "inactive" => IsDeactivated(u),
        "auth" => u.IsInAuth,
        "noauth" => !u.IsInAuth,
        "locked" => IsLocked(u),
        "nodept" => GetDepartmentIds(u).Count == 0,
        _ => true
    };

    protected async Task ClearFilters()
    {
        SearchInput = string.Empty;
        SearchTerm = string.Empty;
        RoleFilters.Clear();
        StatusFilters.Clear();
        DepartmentFilters.Clear();
        InvalidateFilteredUsers();
        await SaveFilterPreferencesAsync();
        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleSearchInput(ChangeEventArgs args)
    {
        SearchInput = args.Value?.ToString() ?? string.Empty;
        if (_searchCts is not null)
            await _searchCts.CancelAsync();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, _searchCts.Token);
            SearchTerm = SearchInput;
            await SaveFilterPreferencesAsync();
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
        }
    }

    protected async Task SaveFilterPreferencesAsync()
    {
        try
        {
            var prefs = new UserFilterPreferences(
                SearchInput,
                RoleFilters.ToList(),
                StatusFilters.ToList(),
                DepartmentFilters.ToList());
            var json = System.Text.Json.JsonSerializer.Serialize(prefs);
            await JS.InvokeVoidAsync("localStorage.setItem", FilterStorageKey, json);
        }
        catch (JSException)
        {
        }
    }

    protected string GetRoleName(TenantUserDto user) => user.Role switch
    {
        PMRolesConst.Tenant.Admin => WebLoc["LevelManager"].Value,
        PMRolesConst.Tenant.Manger => WebLoc["LevelUser"].Value,
        PMRolesConst.Tenant.User => WebLoc["LevelGuest"].Value,
        PMRolesConst.Tenant.Viewer => "Visare",
        _ => string.IsNullOrWhiteSpace(user.Role) ? "—" : user.Role
    };

    protected string PageTitle => DepartmentId.HasValue
        ? WebLoc["DepartmentUsersTitle"]
        : WithoutDepartmentOnly
            ? WebLoc["UsersWithoutDepartmentTitle"]
            : WebLoc["AllTenantUsersTitle"];

    protected override async Task OnParametersSetAsync()
    {
        if (_lastDepartmentId == DepartmentId
            && _lastWithoutDepartmentOnly == WithoutDepartmentOnly
            && Users is not null)
            return;

        _lastDepartmentId = DepartmentId;
        _lastWithoutDepartmentOnly = WithoutDepartmentOnly;
        await LoadAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            var json = await JS.InvokeAsync<string?>("localStorage.getItem", FilterStorageKey);
            var saved = string.IsNullOrWhiteSpace(json)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<UserFilterPreferences>(json);

            if (saved is not null)
            {
                SearchInput = saved.Search ?? string.Empty;
                SearchTerm = SearchInput;

                RoleFilters.Clear();
                foreach (var r in saved.Roles ?? [])
                    RoleFilters.Add(r);

                StatusFilters.Clear();
                foreach (var s in saved.Statuses ?? [])
                    StatusFilters.Add(s);

                DepartmentFilters.Clear();
                foreach (var d in saved.Departments ?? [])
                    DepartmentFilters.Add(d);

                InvalidateFilteredUsers();
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (Exception ex) when (ex is JSException or System.Text.Json.JsonException)
        {
        }
    }

    protected async Task Back()
    {
        if (OnClickCallback.HasDelegate)
            await OnClickCallback.InvokeAsync(false);
    }

    protected async Task LoadAsync()
    {
        if (_loadCts is not null)
            await _loadCts.CancelAsync();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            IsLoading = true;
            LoadError = null;
            await InvokeAsync(StateHasChanged);

            DepartmentsList ??= await Dispatcher.Send(new GetDepartmentsAsListQuery());
            var loadedUsers = await DepartmentUsersViewService.GetUsersAsync(DepartmentId, WithoutDepartmentOnly, ct);
            ct.ThrowIfCancellationRequested();
            Users = loadedUsers ?? [];

            if (TenantMaxUsers == 0)
                TenantMaxUsers = await DepartmentUsersViewService.GetTenantMaxUsersAsync(ct);
            DetailsUser = DetailsUser is null ? null : Users.FirstOrDefault(x => x.Id == DetailsUser.Id);
            ActionMenuUser = ActionMenuUser is null ? null : Users.FirstOrDefault(x => x.Id == ActionMenuUser.Id);
            InvalidateFilteredUsers();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            Users ??= [];
            LoadError = ex.Message;
            InvalidateFilteredUsers();
        }
        finally
        {
            if (!ct.IsCancellationRequested)
            {
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    /// <summary>Registers a new tenant user (moved here from the department toolbar when the tabs were introduced).</summary>
    protected void OpenRegisterUser()
    {
        if (IsBusy)
            return;

        var user = new TenantUserDto();
        if (DepartmentId is > 0)
            user.DepartmentId = DepartmentId;

        MHD.Modal.ShowComponent<UpdateUserUI>(
            "Registrera ny användare",
            new Dictionary<string, object>
            {
                [nameof(UpdateUserUI.UserForm)] = user,
                [nameof(UpdateUserUI.DepartmentId)] = DepartmentId ?? 0,
                [nameof(UpdateUserUI.Callback)] = EventCallback.Factory.Create<bool>(this, OnEditUserResultAsync),
            },
            MhdDialogSize.ExtraLarge,
            DialogButtonsHelper.CreateSaveCancelButtons(UpdateUserUI.DialogFormId));
    }

    protected void ShowDetails(TenantUserDto user)
    {
        CloseActionMenu();
        DetailsUser = user;
    }

    protected void CloseDetails() => DetailsUser = null;

    protected void ToggleActionMenu(TenantUserDto user, MouseEventArgs args)
    {
        if (ActionMenuUser?.Id == user.Id)
        {
            CloseActionMenu();
            return;
        }

        ActionMenuLeftPx = Math.Max(16, args.ClientX - 8);
        ActionMenuTopPx = Math.Max(64, args.ClientY + 8);
        ActionMenuUser = user;
    }

    protected void CloseActionMenu()
        => ActionMenuUser = null;

    protected bool IsActionMenuOpen(TenantUserDto user)
        => ActionMenuUser?.Id == user.Id;

    protected void RunUserAction(TenantUserDto user, Action<TenantUserDto> action)
    {
        CloseActionMenu();
        action(user);
    }

    protected async Task RunUserActionAsync(TenantUserDto user, Func<TenantUserDto, Task> action)
    {
        CloseActionMenu();
        await action(user);
    }

    protected IReadOnlyList<int> GetDepartmentIds(TenantUserDto user)
    {
        if (user.DepartmentIds.Count > 0)
            return user.DepartmentIds;

        return user.DepartmentId.HasValue ? [user.DepartmentId.Value] : [];
    }

    protected string GetDepartmentName(TenantUserDto user)
    {
        var names = GetDepartmentIds(user)
            .Select(id => DepartmentsList?.FirstOrDefault(d => d.Id == id)?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToList();

        return names.Count == 0 ? "—" : string.Join(", ", names);
    }

    protected string GetDepartmentDisplayText(TenantUserDto user)
    {
        var count = GetDepartmentIds(user).Count;
        return count > 2 ? $"{count} avdelningar" : GetDepartmentName(user);
    }

    /// <summary>Account status only; lockout is displayed separately in the Inloggningsspärr column.</summary>
    protected string GetAccountStatusLabel(TenantUserDto user)
        => IsDeactivated(user) ? WebLoc["StatusInactive"].Value
            : IsPending(user) ? WebLoc["InvitePending"].Value
            : WebLoc["StatusActive"].Value;

    protected string GetLockoutStatus(TenantUserDto user)
    {
        if (!IsLocked(user))
            return "Ej spärrad";

        // An indefinite lockout ("Tills vidare") is stored as a far-future end date.
        return user.LockoutEnd!.Value.Year >= 3000
            ? "Spärrad tills vidare"
            : "Spärrad";
    }

    protected static bool IsLocked(TenantUserDto user)
        => user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

    /// <summary>Has a sign-in account that an admin has functionally deactivated.</summary>
    protected static bool IsDeactivated(TenantUserDto user)
        => user.IsInAuth && !user.IsActive;

    /// <summary>Invited (has a sign-in account) but has never logged in, not locked, not deactivated.</summary>
    protected static bool IsPending(TenantUserDto user)
        => (!user.IsInAuth || user.LastLoginAt is null) && user.IsActive;

    /// <summary>Registered, signed in at least once, not locked and not deactivated.</summary>
    protected static bool IsActive(TenantUserDto user)
        => user.IsInAuth && user.IsActive && user.LastLoginAt is not null;

    /// <summary>Active but with no sign-in for <see cref="StaleDays"/> days.</summary>
    protected static bool IsStale(TenantUserDto user)
        => IsActive(user) && user.LastLoginAt!.Value < DateTimeOffset.UtcNow.AddDays(-StaleDays);

    /// <summary>Toggles a summary card's status filter on/off and persists it.</summary>
    protected async Task SetStatusFilter(string status)
    {
        if (string.IsNullOrEmpty(status))
            StatusFilters.Clear();
        else if (!StatusFilters.Add(status))
            StatusFilters.Remove(status);

        InvalidateFilteredUsers();
        await SaveFilterPreferencesAsync();
        await InvokeAsync(StateHasChanged);
    }

    protected static bool IsAdmin(TenantUserDto user)
        => user.IsInAuth && user.Role == PMRolesConst.Tenant.Admin;

    /// <summary>Number of administrators currently visible (only meaningful in the all-users view).</summary>
    protected int AdminCount => (Users ?? Enumerable.Empty<TenantUserDto>()).Count(IsAdmin);

    /// <summary>True when this user is an admin and the only one left, so admin-removing actions must be blocked.</summary>
    protected bool IsLastAdmin(TenantUserDto user) => IsAdmin(user) && AdminCount <= 1;

    private const string LastAdminBlockedTooltip = "Kan inte utföras eftersom detta är sista administratören.";

    protected bool CanEdit(TenantUserDto user)
        => !IsBusy && user.IsInAuth;

    protected bool CanSendConfirmation(TenantUserDto user)
        => !IsBusy && user.IsInAuth && !string.IsNullOrWhiteSpace(user.Email);

    protected bool CanResetPassword(TenantUserDto user)
        => !IsBusy && !string.IsNullOrWhiteSpace(user.IdAuth);

    protected bool CanLock(TenantUserDto user)
        => !IsBusy && !string.IsNullOrWhiteSpace(user.IdAuth) && !IsLocked(user) && !IsLastAdmin(user);

    protected bool CanUnlock(TenantUserDto user)
        => !IsBusy && !string.IsNullOrWhiteSpace(user.IdAuth) && IsLocked(user);

    protected bool CanDeactivate(TenantUserDto user)
        => !IsBusy && !string.IsNullOrWhiteSpace(user.IdAuth) && !IsDeactivated(user) && !IsLastAdmin(user);

    protected bool CanRemoveAuth(TenantUserDto user)
        => !IsBusy && !IsLastAdmin(user) && (user.IsInAuth || !string.IsNullOrWhiteSpace(user.IdAuth));

    protected bool CanDelete(TenantUserDto user)
        => !IsBusy && !IsLastAdmin(user);

    protected string ActionDisabledTitle(
        TenantUserDto user,
        bool enabled,
        string enabledTitle,
        string? disabledTitle = null,
        string? lastAdminTitle = null)
        => enabled ? enabledTitle
            : IsLastAdmin(user) ? lastAdminTitle ?? LastAdminBlockedTooltip
            : disabledTitle ?? string.Empty;

    protected string LockDisabledTitle(TenantUserDto user)
        => string.IsNullOrWhiteSpace(user.IdAuth) ? "Användaren saknar inloggning."
            : IsLocked(user) ? "Användaren är redan spärrad."
            : string.Empty;

    protected string UnlockDisabledTitle(TenantUserDto user)
        => string.IsNullOrWhiteSpace(user.IdAuth) ? "Användaren saknar inloggning."
            : !IsLocked(user) ? "Användaren är inte spärrad."
            : string.Empty;

    protected string DeactivateDisabledTitle(TenantUserDto user)
        => string.IsNullOrWhiteSpace(user.IdAuth) ? "Användaren saknar inloggning."
            : IsDeactivated(user) ? "Kontot är redan inaktivt."
            : string.Empty;

    private void ShowLastAdminBlocked()
        => MHD.MessageOk(WebLoc["LastAdminTitle"].Value, WebLoc["LastAdminMessage"].Value);

    protected string GetDisplayName(TenantUserDto user)
    {
        var displayName = string.Join(' ', new[] { user.Firstname, user.Lastname }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(displayName) ? user.Email ?? user.Username ?? "—" : displayName;
    }

    protected string FormatDate(DateTimeOffset? value)
        => value?.ToLocalTime().ToString("yyyy-MM-dd") ?? "—";

    protected string FormatDateTime(DateTimeOffset? value)
        => value?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "—";

    protected async Task RecreateUserAsync(TenantUserDto user)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var ok = await TenantUserService.RecreateUserAsync(user);
            MHD.Notifications(ok ? ToastType.Add : ToastType.Danger, ok);

            if (ok)
                await LoadAsync();
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected void AskDeleteUser(TenantUserDto user)
    {
        if (IsLastAdmin(user))
        {
            ShowLastAdminBlocked();
            return;
        }

        var label = user.Email ?? user.Username ?? "User";
        MHD.DeleteMessage(label, EventCallback.Factory.Create(this, () => ConfirmDeleteAsync(user)));
    }

    protected void AskRemoveOnlyFromRegister(TenantUserDto user)
    {
        if (IsLastAdmin(user))
        {
            ShowLastAdminBlocked();
            return;
        }

        var label = user.Email ?? user.Username ?? "User";
        MHD.DeleteMessage(label, EventCallback.Factory.Create(this, () => ConfirmRemoveOnlyFromRegisterAsync(user)));
    }

    protected async Task ConfirmDeleteAsync(TenantUserDto user)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var ok = await TenantUserService.RemoveAsync(user.IdAuth ?? string.Empty, false, user.Id);
            MHD.Notifications(ok ? ToastType.Delete : ToastType.Danger, ok);

            if (ok)
                await LoadAsync();
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task ConfirmRemoveOnlyFromRegisterAsync(TenantUserDto user)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var ok = await TenantUserService.RemoveAsync(user.IdAuth ?? string.Empty, true, user.Id);
            MHD.Notifications(ok ? ToastType.Update : ToastType.Danger, ok);

            if (ok)
                await LoadAsync();
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected void EditUser(TenantUserDto user)
    {
        if (IsBusy || !user.IsInAuth) return;

        MHD.Modal.ShowComponent<UpdateUserUI>(
            AppLoc[LocalizerConst.Update, user.Email ?? user.Username ?? string.Empty],
            new Dictionary<string, object>
            {
                [nameof(UpdateUserUI.UserForm)] = user,
                [nameof(UpdateUserUI.DepartmentId)] = DepartmentId ?? 0,
                [nameof(UpdateUserUI.Callback)] = EventCallback.Factory.Create<bool>(this, OnEditUserResultAsync)
            },
            MhdDialogSize.ExtraLarge,
            DialogButtonsHelper.CreateSaveCancelButtons(UpdateUserUI.DialogFormId));
    }

    private async Task OnEditUserResultAsync(bool isSuccess)
    {
        await MHD.Modal.CloseAsync();

        if (isSuccess)
            await LoadAsync();

        await InvokeAsync(StateHasChanged);
    }

    #region Lock / unlock (single)

    protected void AskToggleLock(TenantUserDto user)
    {
        if (string.IsNullOrWhiteSpace(user.IdAuth))
        {
            MHD.Notifications(ToastType.Update, false);
            return;
        }

        var locked = IsLocked(user);

        // Locking the last admin would lock the whole tenant out of administration.
        if (!locked && IsLastAdmin(user))
        {
            ShowLastAdminBlocked();
            return;
        }

        var label = user.Email ?? user.Username ?? "User";
        var title = locked ? WebLoc["UnlockUser"].Value : WebLoc["LockUser"].Value;
        var message = string.Format(
            (locked ? WebLoc["UnlockConfirmFormat"] : WebLoc["LockConfirmFormat"]).Value, label);

        MHD.MessageYesNo(title, message,
            onYes: EventCallback.Factory.Create(this, () => ToggleLockAsync(user, !locked)));
    }

    protected async Task ToggleLockAsync(TenantUserDto user, bool locked)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(user.IdAuth))
            return;

        IsBusy = true;
        try
        {
            await MHD.Modal.CloseAsync();
            var ok = await TenantUserService.SetLockoutAsync(user.IdAuth, locked);
            MHD.Notifications(ToastType.Update, ok);

            if (ok)
                await LoadAsync();
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    #endregion

    #region Reset password

    protected void AskResetPassword(TenantUserDto user)
    {
        if (string.IsNullOrWhiteSpace(user.IdAuth))
        {
            MHD.Notifications(ToastType.Update, false);
            return;
        }

        var label = user.Email ?? user.Username ?? "User";
        MHD.MessageYesNo(WebLoc["ResetPassword"].Value,
            string.Format(WebLoc["ResetPasswordConfirmFormat"].Value, label),
            onYes: EventCallback.Factory.Create(this, () => ResetPasswordAsync(user)));
    }

    protected async Task ResetPasswordAsync(TenantUserDto user)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(user.IdAuth))
            return;

        IsBusy = true;
        try
        {
            await MHD.Modal.CloseAsync();
            var ok = await TenantUserService.ResetPasswordAsync(user.IdAuth);
            MHD.Notifications(ToastType.Update, ok);
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    #endregion

    #region Resend invitation

    protected void AskResendInvitation(TenantUserDto user)
    {
        if (string.IsNullOrWhiteSpace(user.IdAuth))
        {
            MHD.Notifications(ToastType.Update, false);
            return;
        }

        var label = user.Email ?? user.Username ?? "User";
        MHD.MessageYesNo(WebLoc["ResendInvite"].Value,
            string.Format(WebLoc["ResendInviteConfirmFormat"].Value, label),
            onYes: EventCallback.Factory.Create(this, () => ResendInvitationAsync(user)));
    }

    // Re-uses the password-reset flow, which regenerates a temporary password and e-mails it —
    // exactly what a pending invitee needs to receive again.
    protected async Task ResendInvitationAsync(TenantUserDto user)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(user.IdAuth))
            return;

        IsBusy = true;
        try
        {
            await MHD.Modal.CloseAsync();
            var ok = await TenantUserService.ResetPasswordAsync(user.IdAuth);
            MHD.Notifications(ToastType.Update, ok);
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    #endregion

    #region Send confirmation email

    protected void AskSendConfirmation(TenantUserDto user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            MHD.Notifications(ToastType.Update, false);
            return;
        }

        MHD.MessageYesNo("Skicka bekräftelsemail",
            $"Vill du skicka ett bekräftelsemail till {user.Email}? Användaren ombeds bekräfta sina uppgifter för att slutföra registreringen.",
            onYes: EventCallback.Factory.Create(this, () => SendConfirmationAsync(user)));
    }

    protected async Task SendConfirmationAsync(TenantUserDto user)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(user.Email))
            return;

        IsBusy = true;
        try
        {
            await MHD.Modal.CloseAsync();
            await AccountEmailSender.SendInvitationConfirmationAsync(user.Email, GetDisplayName(user));
            MHD.Notifications(ToastType.Update, true);
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    #endregion

    #region Activate / deactivate (functional account state)

    protected void AskToggleActive(TenantUserDto user)
    {
        if (string.IsNullOrWhiteSpace(user.IdAuth))
        {
            MHD.Notifications(ToastType.Update, false);
            return;
        }

        var deactivating = user.IsActive;

        // Deactivating the last admin would leave the tenant without admin access.
        if (deactivating && IsLastAdmin(user))
        {
            ShowLastAdminBlocked();
            return;
        }

        var label = user.Email ?? user.Username ?? "User";
        var title = deactivating ? WebLoc["Deactivate"].Value : WebLoc["Activate"].Value;
        var message = string.Format(
            (deactivating ? WebLoc["DeactivateConfirmFormat"] : WebLoc["ActivateConfirmFormat"]).Value, label);

        MHD.MessageYesNo(title, message,
            onYes: EventCallback.Factory.Create(this, () => ToggleActiveAsync(user, !user.IsActive)));
    }

    protected async Task ToggleActiveAsync(TenantUserDto user, bool active)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(user.IdAuth))
            return;

        IsBusy = true;
        try
        {
            await MHD.Modal.CloseAsync();
            var ok = await TenantUserService.SetActiveAsync(user.IdAuth, active);
            MHD.Notifications(ToastType.Update, ok);

            if (ok)
                await LoadAsync();
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    #endregion

    #region Export

    protected async Task ExportUsersAsync()
        => await ExportUsersAsync(FilteredUsers, "users");

    private async Task ExportUsersAsync(IEnumerable<TenantUserDto> source, string fileName)
    {
        var rows = source.Select(u => new object?[]
        {
            u.Email,
            GetDisplayName(u),
            GetRoleName(u),
            GetDepartmentName(u),
            GetAccountStatusLabel(u),
            GetLockoutStatus(u),
            FormatDateTime(u.LastLoginAt),
            FormatDate(u.LockoutStart),
            FormatDate(u.LockoutEnd),
            u.PhoneNumber
        });

        var columns = new[]
        {
            ResourceIdentity.email,
            CalcResource.name,
            WebLoc["Role"].Value,
            "Avdelningar",
            "Kontostatus",
            "Inloggningsspärr",
            WebLoc["LastLogin"].Value,
            WebLoc["LockoutStart"].Value,
            WebLoc["LockoutEnd"].Value,
            ResourceIdentity.phone
        };

        await ReportExportInterop.ExportExcelAsync(JS, fileName, PageTitle, columns, rows);
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_searchCts is not null)
            await _searchCts.CancelAsync();
        _searchCts?.Dispose();
        if (_loadCts is not null)
            await _loadCts.CancelAsync();
        _loadCts?.Dispose();
    }

    private sealed record UserFilterPreferences(
        string? Search,
        List<string>? Roles,
        List<string>? Statuses,
        List<int>? Departments);
}
