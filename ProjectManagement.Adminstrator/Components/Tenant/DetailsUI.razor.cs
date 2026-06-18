using AuthPermissions.Entity;
using BlazorMHD.UI.Core.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Adminstrator.Components.AppUser;
using ProjectManagement.Adminstrator.Constants;
using ProjectManagement.Adminstrator.Services.MHDBlazor;
using ProjectManagement.Adminstrator.Shared.ResourceFiles.AppControll;
using ProjectManagement.Adminstrator.Shared.ResourceFiles.Identity;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Adminstrator.Components.Tenant
{
    public partial class DetailsUI : AppComponentBase
    {
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        [Parameter] public int Id { get; set; }
        [Parameter] public EventCallback<bool> Callback { get; set; }

        TenantEntity? TenantPut;
        List<ApplicationUser>? Users;
        bool CanManageTenantUsers;
        Dictionary<int, string> DepartmentNames = [];

        protected string DepartmentName(int? id)
            => id.HasValue && DepartmentNames.TryGetValue(id.Value, out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : "—";

        protected static bool IsLocked(ApplicationUser user)
            => user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

        protected int LockedUsersCount => Users?.Count(IsLocked) ?? 0;

        async Task Context(ApplicationUser item)
        {
            if (!CanManageTenantUsers)
            {
                return;
            }

            var list = new List<MhdContextMenuItem>
            {
                new() { Label = ResourceApp.edit, OnClickAsync = () => { UserRoleDialog(item); return Task.CompletedTask; } },
            };

            if (IsLocked(item))
                list.Add(new() { Label = $"🔓 {AppControll.blockout}", OnClickAsync = () => { AskSetLockout(item, false); return Task.CompletedTask; } });
            else
                list.Add(new() { Label = $"🔒 {AppControll.block}", OnClickAsync = () => { AskSetLockout(item, true); return Task.CompletedTask; } });

            list.Add(new() { Label = $"🔑 {ResourceIdentity.ResetPassword}", OnClickAsync = () => { AskResetPassword(item); return Task.CompletedTask; } });
            list.Add(new() { Label = ResourceApp.delete, OnClickAsync = () => { AskRemove(item); return Task.CompletedTask; } });

            await ContextService.ShowMenuAsync(list);
        }

        void AskSetLockout(ApplicationUser user, bool block)
        {
            MHD.MessageYesNo(
                AppControll.block,
                block ? AppControll.confirmTenantBlock : AppControll.confirmTenantBlockout,
                MhdState.Warning,
                EventCallback.Factory.Create(this, () => SetLockoutAsync(user, block)));
        }

        async Task SetLockoutAsync(ApplicationUser user, bool block)
        {
            await Modal.CloseAsync();
            var result = await ExHandlers.RunCheckTokenAsync(() => UsersService.SetTenantUserLockoutAsync(user.Id, Id, block));
            MHD.ToastMessage(AppControll.block, ToastType.Update, result);
            if (result)
                await GetUsersAsync();
        }

        void AskResetPassword(ApplicationUser user)
        {
            MHD.MessageYesNo(
                ResourceIdentity.ResetPassword,
                AppControll.confirmResetPassword,
                MhdState.Warning,
                EventCallback.Factory.Create(this, () => ResetPasswordAsync(user)));
        }

        async Task ResetPasswordAsync(ApplicationUser user)
        {
            await Modal.CloseAsync();
            var result = await ExHandlers.RunCheckTokenAsync(() => UsersService.ResetUserPasswordAsync(user.Id, Id));
            MHD.ToastMessage(ResourceIdentity.ResetPassword, ToastType.Update, result);
        }

        void UserRoleDialog(ApplicationUser user) =>
            Modal.ShowComponent<UpdateUserUI>(AppLoc[LocalizerConst.Update, user.Email ?? string.Empty], new Dictionary<string, object>
            {
                [nameof(UpdateUserUI.UserForm)] = user,
                [nameof(UpdateUserUI.TenantId)] = TenantPut?.Id ?? 0,
                [nameof(UpdateUserUI.Callback)] = EventCallback.Factory.Create<bool>(this, RefreshAsync),
            });

        async Task RefreshAsync(bool isSuccess)
        {
            if (isSuccess)
            {
                await GetUsersAsync();
            }
            await Modal.CloseAsync();
        }

        private async Task RemoveAsync(ApplicationUser deleteConfirmed)
        {
            if (await ExHandlers.RunCheckTokenAsync(() => UsersService.RemoveUserAsync(deleteConfirmed.Id, Id)))
            {
                Users?.Remove(deleteConfirmed);
                StateHasChanged();
            }
        }

        // Shows a confirmation dialog; the actual delete runs in RemoveAsync after the user confirms.
        void AskRemove(ApplicationUser user)
        {
            MHD.MessageYesNo(ResourceApp.delete, AppLoc[LocalizerConst.deleteConfirmMsg, user.Email ?? string.Empty], MhdState.Warning, EventCallback.Factory.Create(this, () => RemoveAsync(user)));
        }

        async Task GetUsersAsync()
        {
            Users = await ExHandlers.RunCheckTokenAsync(() => UsersService.GetUsersAsync(Id));
            StateHasChanged();
        }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var principal = authState.User;
            CanManageTenantUsers = principal.IsInRole(PMRolesConst.APP.Admin) || principal.IsInRole(PMRolesConst.APP.Manger);

            using var appContext = ContextFactory.CreateDbContext();
            if (Id > 0)
            {
                TenantPut = await ExHandlers.RunCheckTokenAsync(() => UsersService.GetByIdAsync(Id));
                if (TenantPut is not null)
                {
                    TenantPut.TenantDB = await appContext.TenantDatabase.FirstOrDefaultAsync(a => a.Id == TenantPut.TenantDBId);
                }

                // Resolve department names from the tenant DB so the users table shows names, not ids.
                DepartmentNames = await ExHandlers.RunCheckTokenAsync(() => UsersService.GetDepartmentNamesAsync(Id)) ?? [];

                // Load linked users up front so usage is visible without an extra click.
                await GetUsersAsync();
            }
        }
    }
}
