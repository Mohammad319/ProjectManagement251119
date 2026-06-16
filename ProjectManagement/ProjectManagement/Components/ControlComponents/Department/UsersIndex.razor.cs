using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared;
using BlazorMHD.UI.Core.Services;
using ProjectManagement.Client.Helper;
using Domain.DTO.User;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Services;
using ProjectManagement.Services.UI;

namespace ProjectManagement.Components.ControlComponents.Department;

public partial class UsersIndex
{
    [Parameter] public int? DepartmentId { get; set; }
    [Parameter] public bool WithoutDepartmentOnly { get; set; }
    [Parameter] public EventCallback<bool> OnClickCallback { get; set; }

    [Inject] public ITenantUserService TenantUserService { get; set; } = default!;
    [Inject] public IDepartmentUsersViewService DepartmentUsersViewService { get; set; } = default!;
    [Inject] public IStringLocalizer<PMWebResource> WebLoc { get; set; } = default!;

    protected List<TenantUserDto>? Users { get; set; }
    protected bool IsLoading { get; set; } = true;
    protected bool IsBusy { get; set; }

    private int? _lastDepartmentId;
    private bool _lastWithoutDepartmentOnly;

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

    protected async Task Back()
    {
        if (OnClickCallback.HasDelegate)
            await OnClickCallback.InvokeAsync(false);
    }

    protected async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            await InvokeAsync(StateHasChanged);

            Users = await DepartmentUsersViewService.GetUsersAsync(DepartmentId, WithoutDepartmentOnly);
            Users ??= [];
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected string GetDisplayName(TenantUserDto user)
    {
        var displayName = string.Join(' ', new[] { user.Firstname, user.Lastname }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(displayName) ? user.Email ?? user.Username ?? "—" : displayName;
    }

    protected string FormatDate(DateTimeOffset? value)
        => value?.ToLocalTime().ToString("yyyy-MM-dd") ?? "—";

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
}
