using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Shared;
using ProjectManagement.Shared.Constant;
using Application.Feature.Identity.Department.Queries;
using Domain.DTO.User;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using ProjectManagement.Services;
using ProjectManagement.Shared.DTO.General;

namespace ProjectManagement.Components.ControlComponents.Department;

using AuthPermissions.Context;
using ProjectManagement.Components.Shared;

public partial class UpdateUserUI : AppComponentBase
{
    [Parameter] public EventCallback<bool> Callback { get; set; }
    [Parameter] public TenantUserDto UserForm { get; set; } = default!;
    [Parameter] public int? DepartmentId { get; set; }

    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private ITenantUserService TenantUserService { get; set; } = default!;
    [Inject] private IStringLocalizer<PMWebResource> WebLoc { get; set; } = default!;

    protected TenantUserDto? Form { get; set; }
    protected List<ListDTO>? Departments { get; set; }

    protected bool IsLoading { get; set; } = true;
    protected bool IsSaving { get; set; }
    protected bool IsBusy => IsLoading || IsSaving;

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;

        try
        {
            if (DepartmentId == 0)
                DepartmentId = null;

            Departments = await Dispatcher.Send(new GetDepartmentsAsListQuery());

            var sourceUser = UserForm ?? new TenantUserDto();
            Form = new TenantUserDto
            {
                Email = sourceUser.Email,
                Firstname = sourceUser.Firstname,
                Lastname = sourceUser.Lastname,
                DepartmentId = DepartmentId ?? sourceUser.DepartmentId,
                LockoutEnabled = sourceUser.LockoutEnabled,
                IdAuth = sourceUser.IdAuth,
                LockoutStart = sourceUser.LockoutStart,
                LockoutEnd = sourceUser.LockoutEnd,
                PhoneNumber = sourceUser.PhoneNumber,
                PhoneNumberConfirmed = sourceUser.PhoneNumberConfirmed,
                Id = sourceUser.Id,
                Username = sourceUser.Username,
                Role = sourceUser.Role
            };

            await InitDefaultsAsync();
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task InitDefaultsAsync()
    {
        if (Form is null)
            return;

        if (!Form.DepartmentId.HasValue && Departments?.Any() == true)
            Form.DepartmentId = Departments[0].Id;

        var resolvedRole = await ResolveExistingRoleAsync();
        Form.Role = !string.IsNullOrWhiteSpace(resolvedRole)
            ? resolvedRole
            : Form.Role ?? PMRolesConst.Tenant.Manger;
    }

    private async Task<string?> ResolveExistingRoleAsync()
    {
        if (UserForm.Id <= 0)
            return null;

        ApplicationUser? authUser = null;

        if (!string.IsNullOrWhiteSpace(UserForm.IdAuth))
            authUser = await UserManager.FindByIdAsync(UserForm.IdAuth);

        if (authUser is null && !string.IsNullOrWhiteSpace(UserForm.Username))
            authUser = await UserManager.FindByNameAsync(UserForm.Username);

        if (authUser is null && !string.IsNullOrWhiteSpace(UserForm.Email))
            authUser = await UserManager.FindByEmailAsync(UserForm.Email);

        if (authUser is null)
            return null;

        var roles = await UserManager.GetRolesAsync(authUser);
        return roles.FirstOrDefault();
    }

    protected void ClearLockoutStart()
    {
        if (Form is null) return;
        Form.LockoutStart = null;
    }

    protected void ClearLockoutEnd()
    {
        if (Form is null) return;
        Form.LockoutEnd = null;
    }

    protected async Task Cancel()
    {
        if (Callback.HasDelegate)
            await Callback.InvokeAsync(false);
    }

    protected async Task HandleSubmitAsync()
    {
        if (Form is null)
        {
            if (Callback.HasDelegate)
                await Callback.InvokeAsync(false);
            return;
        }

        if (IsSaving)
            return;

        if (Form.Role == PMRolesConst.Tenant.Admin)
            Form.DepartmentId = null;
        else if (!Form.DepartmentId.HasValue)
        {
            MHD.Notifications(ToastType.Danger, false);
            return;
        }

        IsSaving = true;

        try
        {
            Form.Email = Form.Email?.Trim();
            Form.Firstname = Form.Firstname?.Trim();
            Form.Lastname = Form.Lastname?.Trim();
            Form.PhoneNumber = Form.PhoneNumber?.Trim();
            Form.Username = string.IsNullOrWhiteSpace(Form.Username) ? Form.Email : Form.Username?.Trim();

            bool ok = UserForm.Id == 0
                ? await TenantUserService.RegisterAsync(Form)
                : await TenantUserService.UpdateUserAsync(Form);

            MHD.Notifications(UserForm.Id == 0 ? ToastType.Add : ToastType.Update, ok);

            if (Callback.HasDelegate)
                await Callback.InvokeAsync(ok);
        }
        finally
        {
            IsSaving = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}