using AuthPermissions.Entity;
using ContextMenuMHD;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Adminstrator.Components.AppUser;
using ProjectManagement.Adminstrator.Constants;

namespace ProjectManagement.Adminstrator.Components.Tenant
{
    public partial class DetailsUI : AppComponentBase
    {
        async Task Context(ApplicationUser item)
        {
            // if (!(user.Identity?.IsAuthenticated == true &&
            //          user.IsInRole(PMRolesConst.APP.Admin))) return; // No additional options for admin
            var list = new List<MenuItem>()
             {
                        new() { Label = ResourceApp.edit, OnClickAsync = () => { UserRoleDialog(item); return Task.CompletedTask; } },
                        new() { Label = ResourceApp.delete, OnClickAsync = () => {Remove(item); return Task.CompletedTask;}},
             };
            await ContextService.ShowMenuAsync(list);
        }
        [Parameter] public int Id { get; set; }
        TenantEntity? TenantPut;
        List<ApplicationUser>? Users;
        bool IsAddingCompanyInfo;
        [Parameter] public EventCallback<bool> Callback { get; set; }
        void UserRoleDialog(ApplicationUser user) =>
    Modal.ShowComponent<UpdateUserUI>(AppLoc[LocalizerConst.Update, user.Email ?? string.Empty], new Dictionary<string, object>
    {
        [nameof(UpdateUserUI.UserForm)] = user,
        [nameof(UpdateUserUI.TenantId)] = TenantPut?.Id ?? 0,
        [nameof(UpdateUserUI.Callback)] = EventCallback.Factory.Create<bool>(this, RefreshAsync),
    });
        async Task RefreshAsync(bool IsSuccess)
        {
            if (IsSuccess == true)
            {
                await GetUsersAsync();
            }
            Modal.Close();
        }
        private async Task RemoveAsync(ApplicationUser deleteConfirmed)
        {
            if (await ExHandlers.RunCheckTokenAsync(() => UsersService.RemoveUserAsync((deleteConfirmed).Id, Id)))
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
        async Task AddBasicCompanyInfoAsync()
        {
            if (Id <= 0 || IsAddingCompanyInfo) return;

            IsAddingCompanyInfo = true;
            try
            {
                var result = await ExHandlers.RunCheckTokenAsync(() => UsersService.AddBasicCompanyInfoAsync(Id));
                MHD.MessageOk(ResourceApp.newItem, result ? "Basic company info added successfully." : "No data was added (already exists).", result ? MhdState.Success : MhdState.Info);
            }
            finally
            {
                IsAddingCompanyInfo = false;
                StateHasChanged();
            }
        }

        string DB = string.Empty;
        protected async override Task OnInitializedAsync()
        {
            using var _appContext = ContextFactory.CreateDbContext();
            if (Id > 0)
            {
                TenantPut = await ExHandlers.RunCheckTokenAsync(() => UsersService.GetByIdAsync(Id));
                if (TenantPut is not null)
                {
                    DB = (await _appContext.TenantDatabase.FirstOrDefaultAsync(a => a.Id == TenantPut.TenantDBId))?.Name ?? string.Empty;
                }
            }
        }
    }
}
