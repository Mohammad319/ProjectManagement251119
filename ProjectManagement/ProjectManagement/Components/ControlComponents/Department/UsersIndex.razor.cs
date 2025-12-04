using AuthPermissions.Context;
using DocumentFormat.OpenXml.Spreadsheet;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;

namespace ProjectManagement.Components.ControlComponents.Department
{
    public class TenantUserDto
    {
        public int Id { get; set; }
        public string? IdAuth { get; set; }
        public string? Role { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public int? DepartmentId { get; set; }
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public bool IsInAuth { get; set; }
        public DateTimeOffset? LockoutStart { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
    }

    public partial class UsersIndex
    {
        [Parameter] public int? DepartmentId { get; set; }
        List<TenantUserDto>? Users;

        bool Loading = false;
        [Parameter] public EventCallback<bool> OnClickCallback { get; set; }
        void UserRoleDialog(TenantUserDto userdto)
        {
            MHD.Modal.ShowComponent<UpdateUserUI>("Update", new Dictionary<string, object>
            {
                [nameof(UpdateUserUI.UserForm)] = userdto,
                [nameof(UpdateUserUI.DepartmentId)] = DepartmentId,
                [nameof(UpdateUserUI.Callback)] = EventCallback.Factory.Create<bool>(this, RefreshAsync),
            }, BlazorMHD.UI.Core.Services.DialogSize.ExtraLarge);
        }
        async Task RefreshAsync(bool IsSuccess)
        {
            if (IsSuccess == true)
            {
                Loading = true;
                await GetAsync();
                Loading = false;
            }
            MHD.Modal.Close();
        }
        async Task RemoveAsync(string id, bool onlyfromregister, int userid)
        {
            await TenantUserService.RemoveAsync(id, onlyfromregister, userid);
            await GetAsync();
            StateHasChanged();
        }
        void Delete_Click(TenantUserDto user)
        {
            MHD.DeleteMessage(user.Email, EventCallback.Factory.Create(this, () => RemoveAsync(user.IdAuth, false, user.Id)));
        }
        void DeleteOnlyFRomRegister_Click(TenantUserDto user)
        {
            MHD.DeleteMessage(user.Email, EventCallback.Factory.Create(this, () => RemoveAsync(user.IdAuth, true, user.Id)));
        }
        protected override async Task OnInitializedAsync()
        {
            Loading = true;
            await GetAsync();
            Loading = false;
        }

        async Task GetAsync()
        {
            if (DepartmentId == 0) DepartmentId = null;
            Users = await TenantUserService.GetAllTenantUsersAsync(DepartmentId);
        }
    }
}
