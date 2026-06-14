using AuthPermissions.Entity;
using BlazorMHD.UI.Core.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Adminstrator.Components.AppUser;
using ProjectManagement.Adminstrator.Constants;
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

        async Task Context(ApplicationUser item)
        {
            if (!CanManageTenantUsers)
            {
                return;
            }

            var list = new List<ContextMenuItem>
            {
                new() { Label = ResourceApp.edit, OnClickAsync = () => { UserRoleDialog(item); return Task.CompletedTask; } },
                new() { Label = ResourceApp.delete, OnClickAsync = () => { Remove(item); return Task.CompletedTask; } },
            };
            await ContextService.ShowMenuAsync(list);
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
            Modal.Close();
        }

        private async Task RemoveAsync(ApplicationUser deleteConfirmed)
        {
            if (await ExHandlers.RunCheckTokenAsync(() => UsersService.RemoveUserAsync(deleteConfirmed.Id, Id)))
            {
                Users?.Remove(deleteConfirmed);
                StateHasChanged();
            }
        }

        void Remove(ApplicationUser user)
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
            }
        }
    }
}
