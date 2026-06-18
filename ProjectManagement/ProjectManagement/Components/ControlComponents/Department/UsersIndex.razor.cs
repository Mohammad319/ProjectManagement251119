using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared;
using ProjectManagement.Shared.Constant;
using BlazorMHD.UI.Core.Services;
using ProjectManagement.Client.Helper;
using Domain.DTO.User;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Services;
using ProjectManagement.Services.UI;
using Application.Feature.Identity.Department.Queries;
using ProjectManagement.Shared.DTO.General;
using Microsoft.JSInterop;
using ProjectManagement.Client.Shared.ResourceFiles.Identity;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Department;

public partial class UsersIndex : IAsyncDisposable
{
    private const string FilterStorageKey = "DepartmentUsers.Filters";

    [Parameter] public int? DepartmentId { get; set; }
    [Parameter] public bool WithoutDepartmentOnly { get; set; }
    [Parameter] public EventCallback<bool> OnClickCallback { get; set; }

    [Inject] public ITenantUserService TenantUserService { get; set; } = default!;
    [Inject] public IDepartmentUsersViewService DepartmentUsersViewService { get; set; } = default!;
    [Inject] public IUserManagementAuditService UserAuditService { get; set; } = default!;
    [Inject] public IStringLocalizer<PMWebResource> WebLoc { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;

    protected List<TenantUserDto>? Users { get; set; }
    protected bool IsLoading { get; set; } = true;
    protected bool IsBusy { get; set; }
    protected string? LoadError { get; set; }
    protected TenantUserDto? DetailsUser { get; set; }
    protected IReadOnlyList<UserManagementAuditItem> AuditEntries { get; set; } = [];
    protected bool IsLoadingAudit { get; set; }
    protected string? AuditLoadError { get; set; }

    private int? _lastDepartmentId;
    private bool _lastWithoutDepartmentOnly;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _searchCts;
    private List<TenantUserDto>? _filteredCache;
    private string _searchTerm = string.Empty;
    private string _roleFilter = string.Empty;
    private string _statusFilter = string.Empty;

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

    protected string RoleFilter
    {
        get => _roleFilter;
        set
        {
            if (_roleFilter == value) return;
            _roleFilter = value;
            InvalidateFilteredUsers();
        }
    }

    protected string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (_statusFilter == value) return;
            _statusFilter = value;
            InvalidateFilteredUsers();
        }
    }

    // Bulk-selection state
    protected HashSet<int> SelectedIds { get; } = new();
    protected List<ListDTO>? DepartmentsList { get; set; }
    protected int? BulkDepartmentId { get; set; }
    protected string BulkRole { get; set; } = string.Empty;

    /// <summary>Accounts considered inactive when their last sign-in is older than this many days.</summary>
    private const int StaleDays = 90;

    protected int TotalCount => Users?.Count ?? 0;
    protected int RegisteredCount => Users?.Count(x => x.IsInAuth) ?? 0;
    protected int LockedCount => Users?.Count(IsLocked) ?? 0;
    protected int WithoutDepartmentCount => Users?.Count(x => !x.DepartmentId.HasValue) ?? 0;
    protected int ActiveCount => Users?.Count(IsActive) ?? 0;
    protected int PendingCount => Users?.Count(IsPending) ?? 0;
    protected int StaleCount => Users?.Count(IsStale) ?? 0;
    protected int NoAuthCount => Users?.Count(x => !x.IsInAuth) ?? 0;
    protected int InactiveCount => Users?.Count(IsDeactivated) ?? 0;

    /// <summary>The tenant's licensed user limit (0 = unknown/not enforced in the UI).</summary>
    protected int TenantMaxUsers { get; private set; }
    protected bool ShowCapacity => TenantMaxUsers > 0;
    protected bool CapacityReached => ShowCapacity && RegisteredCount >= TenantMaxUsers;
    protected bool CapacityNearLimit => ShowCapacity && !CapacityReached && RegisteredCount >= TenantMaxUsers - 1;

    // Role distribution (constant names map to tiers by position: Admin=Manager, Manger=User, User=Guest).
    protected int ManagerCount => Users?.Count(x => x.Role == PMRolesConst.Tenant.Admin) ?? 0;
    protected int MemberCount => Users?.Count(x => x.Role == PMRolesConst.Tenant.Manger) ?? 0;
    protected int GuestCount => Users?.Count(x => x.Role == PMRolesConst.Tenant.User) ?? 0;

    protected bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchTerm)
        || !string.IsNullOrEmpty(RoleFilter)
        || !string.IsNullOrEmpty(StatusFilter);

    protected IReadOnlyList<TenantUserDto> FilteredUsers
        => _filteredCache ??= ComputeFilteredUsers();

    private List<TenantUserDto> ComputeFilteredUsers()
    {
        IEnumerable<TenantUserDto> query = Users ?? Enumerable.Empty<TenantUserDto>();

        if (!string.IsNullOrEmpty(RoleFilter))
            query = query.Where(u => string.Equals(u.Role, RoleFilter, StringComparison.OrdinalIgnoreCase));

        query = StatusFilter switch
        {
            "active" => query.Where(IsActive),
            "pending" => query.Where(IsPending),
            "stale" => query.Where(IsStale),
            "inactive" => query.Where(IsDeactivated),
            "auth" => query.Where(u => u.IsInAuth),
            "noauth" => query.Where(u => !u.IsInAuth),
            "locked" => query.Where(IsLocked),
            "nodept" => query.Where(u => !u.DepartmentId.HasValue),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim();
            query = query.Where(u =>
                MatchesTerm(u.Email, term)
                || MatchesTerm(u.Username, term)
                || MatchesTerm(u.Firstname, term)
                || MatchesTerm(u.Lastname, term)
                || MatchesTerm(u.PhoneNumber, term));
        }

        return query.ToList();
    }

    private void InvalidateFilteredUsers() => _filteredCache = null;

    private static bool MatchesTerm(string? value, string term)
        => !string.IsNullOrEmpty(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    protected async Task ClearFilters()
    {
        SearchInput = string.Empty;
        SearchTerm = string.Empty;
        RoleFilter = string.Empty;
        StatusFilter = string.Empty;
        await SaveFilterPreferencesAsync();
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
            var json = System.Text.Json.JsonSerializer.Serialize(new UserFilterPreferences(SearchInput, RoleFilter, StatusFilter));
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
                RoleFilter = saved.Role ?? string.Empty;
                StatusFilter = saved.Status ?? string.Empty;
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
            await LoadAuditAsync(ct);
            DetailsUser = DetailsUser is null ? null : Users.FirstOrDefault(x => x.Id == DetailsUser.Id);
            SelectedIds.Clear();
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

    protected async Task RefreshAuditAsync()
    {
        if (IsLoadingAudit)
            return;

        try
        {
            IsLoadingAudit = true;
            await LoadAuditAsync();
        }
        finally
        {
            IsLoadingAudit = false;
        }
    }

    private async Task LoadAuditAsync(CancellationToken ct = default)
    {
        try
        {
            AuditLoadError = null;
            AuditEntries = await UserAuditService.GetRecentAsync(50, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            AuditEntries = [];
            AuditLoadError = ex.Message;
        }
    }

    protected string GetAuditActionLabel(string action) => action switch
    {
        "user.created" => WebLoc["AuditUserCreated"].Value,
        "user.registered" => WebLoc["AuditUserRegistered"].Value,
        "user.auth-recreated" => WebLoc["AuditAuthRecreated"].Value,
        "user.updated" => WebLoc["AuditUserUpdated"].Value,
        "user.unregistered" => WebLoc["AuditUserUnregistered"].Value,
        "user.deleted" => WebLoc["AuditUserDeleted"].Value,
        "user.password-reset" => WebLoc["AuditPasswordReset"].Value,
        "user.locked" => WebLoc["AuditUserLocked"].Value,
        "user.unlocked" => WebLoc["AuditUserUnlocked"].Value,
        "user.deactivated" => WebLoc["AuditUserDeactivated"].Value,
        "user.activated" => WebLoc["AuditUserActivated"].Value,
        "user.department-changed" => WebLoc["AuditDepartmentChanged"].Value,
        "user.role-changed" => WebLoc["AuditRoleChanged"].Value,
        _ => action
    };

    protected void ShowDetails(TenantUserDto user) => DetailsUser = user;
    protected void CloseDetails() => DetailsUser = null;

    protected static bool IsLocked(TenantUserDto user)
        => user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

    /// <summary>Has a sign-in account that an admin has functionally deactivated.</summary>
    protected static bool IsDeactivated(TenantUserDto user)
        => user.IsInAuth && !user.IsActive;

    /// <summary>Invited (has a sign-in account) but has never logged in, not locked, not deactivated.</summary>
    protected static bool IsPending(TenantUserDto user)
        => user.IsInAuth && user.IsActive && user.LastLoginAt is null && !IsLocked(user);

    /// <summary>Registered, signed in at least once, not locked and not deactivated.</summary>
    protected static bool IsActive(TenantUserDto user)
        => user.IsInAuth && user.IsActive && user.LastLoginAt is not null && !IsLocked(user);

    /// <summary>Active but with no sign-in for <see cref="StaleDays"/> days.</summary>
    protected static bool IsStale(TenantUserDto user)
        => IsActive(user) && user.LastLoginAt!.Value < DateTimeOffset.UtcNow.AddDays(-StaleDays);

    /// <summary>Toggles a summary card's status filter on/off and persists it.</summary>
    protected async Task SetStatusFilter(string status)
    {
        StatusFilter = string.Equals(StatusFilter, status, StringComparison.Ordinal) ? string.Empty : status;
        await SaveFilterPreferencesAsync();
        await InvokeAsync(StateHasChanged);
    }

    protected static bool IsAdmin(TenantUserDto user)
        => user.IsInAuth && user.Role == PMRolesConst.Tenant.Admin;

    /// <summary>Number of administrators currently visible (only meaningful in the all-users view).</summary>
    protected int AdminCount => (Users ?? Enumerable.Empty<TenantUserDto>()).Count(IsAdmin);

    /// <summary>True when this user is an admin and the only one left, so admin-removing actions must be blocked.</summary>
    protected bool IsLastAdmin(TenantUserDto user) => IsAdmin(user) && AdminCount <= 1;

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

    #region Selection

    protected bool IsSelected(int id) => SelectedIds.Contains(id);

    protected void ToggleSelected(int id, bool selected)
    {
        if (selected)
            SelectedIds.Add(id);
        else
            SelectedIds.Remove(id);
    }

    protected bool AllFilteredSelected
        => FilteredUsers.Count > 0 && FilteredUsers.All(u => SelectedIds.Contains(u.Id));

    protected void ToggleSelectAll(bool selected)
    {
        foreach (var user in FilteredUsers)
        {
            if (selected)
                SelectedIds.Add(user.Id);
            else
                SelectedIds.Remove(user.Id);
        }
    }

    protected void ClearSelection() => SelectedIds.Clear();

    protected IReadOnlyList<TenantUserDto> SelectedUsers
        => (Users ?? Enumerable.Empty<TenantUserDto>()).Where(u => SelectedIds.Contains(u.Id)).ToList();

    #endregion

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

    protected async Task ExportSelectedUsersAsync()
        => await ExportUsersAsync(SelectedUsers, "selected-users");

    private async Task ExportUsersAsync(IEnumerable<TenantUserDto> source, string fileName)
    {
        var rows = source.Select(u => new object?[]
        {
            u.Email,
            GetDisplayName(u),
            GetRoleName(u),
            u.IsInAuth ? WebLoc["UserRegistered"].Value : WebLoc["UserMissingAuth"].Value,
            FormatDateTime(u.LastLoginAt),
            IsLocked(u) ? WebLoc["Enabled"].Value : WebLoc["Disabled"].Value,
            FormatDate(u.LockoutEnd),
            u.PhoneNumber
        });

        var columns = new[]
        {
            ResourceIdentity.email,
            CalcResource.name,
            WebLoc["Role"].Value,
            WebLoc["AuthStatus"].Value,
            WebLoc["LastLogin"].Value,
            WebLoc["Lockout"].Value,
            WebLoc["LockoutEnd"].Value,
            ResourceIdentity.phone
        };

        await ReportExportInterop.ExportExcelAsync(JS, fileName, PageTitle, columns, rows);
    }

    #endregion

    #region Bulk actions

    protected void AskBulkLock(bool locked)
    {
        var targets = SelectedUsers.Where(u => !string.IsNullOrWhiteSpace(u.IdAuth)).ToList();
        if (targets.Count == 0)
        {
            MHD.Notifications(ToastType.Update, false);
            return;
        }

        var message = string.Format(WebLoc["BulkConfirmFormat"].Value, targets.Count);
        MHD.MessageYesNo(locked ? WebLoc["BulkLock"].Value : WebLoc["BulkUnlock"].Value, message,
            onYes: EventCallback.Factory.Create(this, () => BulkLockAsync(locked)));
    }

    protected async Task BulkLockAsync(bool locked)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            await MHD.Modal.CloseAsync();
            var ids = SelectedUsers
                .Where(u => !string.IsNullOrWhiteSpace(u.IdAuth))
                .Select(u => u.IdAuth!)
                .ToList();

            var result = await TenantUserService.SetLockoutBulkAsync(ids, locked);
            ShowBulkResult(locked ? WebLoc["BulkLock"].Value : WebLoc["BulkUnlock"].Value, result);
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task BulkMoveAsync()
    {
        if (IsBusy)
            return;

        var ids = SelectedUsers.Select(u => u.Id).Where(id => id > 0).ToList();
        if (ids.Count == 0)
            return;

        IsBusy = true;
        try
        {
            var count = await TenantUserService.SetDepartmentAsync(ids, BulkDepartmentId);
            MHD.Notifications(ToastType.Update, count > 0);
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected void AskBulkRole()
    {
        if (string.IsNullOrEmpty(BulkRole))
            return;

        var targets = SelectedUsers.Where(u => !string.IsNullOrWhiteSpace(u.IdAuth)).ToList();
        if (targets.Count == 0)
        {
            MHD.Notifications(ToastType.Update, false);
            return;
        }

        var message = string.Format(WebLoc["BulkRoleConfirmFormat"].Value, targets.Count);
        MHD.MessageYesNo(WebLoc["BulkChangeRole"].Value, message,
            onYes: EventCallback.Factory.Create(this, BulkRoleAsync));
    }

    protected async Task BulkRoleAsync()
    {
        if (IsBusy || string.IsNullOrEmpty(BulkRole))
            return;

        IsBusy = true;
        try
        {
            await MHD.Modal.CloseAsync();
            var ids = SelectedUsers
                .Where(u => !string.IsNullOrWhiteSpace(u.IdAuth))
                .Select(u => u.IdAuth!)
                .ToList();

            var result = await TenantUserService.SetRoleBulkAsync(ids, BulkRole);
            ShowBulkResult(WebLoc["BulkChangeRole"].Value, result);
            BulkRole = string.Empty;
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected void AskBulkDelete()
    {
        var targets = SelectedUsers;
        if (targets.Count == 0)
            return;

        var message = string.Format(WebLoc["BulkDeleteConfirmFormat"].Value, targets.Count);
        MHD.MessageYesNo(WebLoc["BulkDelete"].Value, message,
            onYes: EventCallback.Factory.Create(this, BulkDeleteAsync));
    }

    protected async Task BulkDeleteAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            await MHD.Modal.CloseAsync();
            var targets = SelectedUsers
                .Select(u => (u.IdAuth, u.Id))
                .ToList();

            var result = await TenantUserService.RemoveBulkAsync(targets);
            ShowBulkResult(WebLoc["BulkDelete"].Value, result, ToastType.Delete);
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>Toast for the overall outcome, plus a detail dialog when some users failed or were protected.</summary>
    private void ShowBulkResult(string title, BulkUserActionResult result, ToastType toast = ToastType.Update)
    {
        MHD.Notifications(toast, result.Succeeded > 0);

        if (result.Failed > 0 || result.SkippedProtected > 0)
        {
            MHD.MessageOk(title,
                string.Format(WebLoc["BulkResultFormat"].Value, result.Succeeded, result.Failed, result.SkippedProtected));
        }
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

    private sealed record UserFilterPreferences(string? Search, string? Role, string? Status);
}
