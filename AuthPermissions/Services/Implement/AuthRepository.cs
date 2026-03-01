using AuthPermissions.Context;
using Domain.Repository.AuthPermissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Models.Account;
namespace AuthPermissions.Services.Implement
{
    public class AuthRepository(ApplicationDbContext _appContext, UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole> _roleManager) : IAuthRepository
    {
        public static string[] GetRoles()
        {
            return [PMRolesConst.APP.Admin, PMRolesConst.APP.SuperManger, PMRolesConst.APP.Manger, PMRolesConst.APP.User,
            PMRolesConst.Tenant.Admin,PMRolesConst.Tenant.SuperManger,PMRolesConst.Tenant.Manger,PMRolesConst.Tenant.User];
        }
        public async Task<bool> Initialize(string email, string pass)
        {
            foreach (var role in GetRoles())
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole { Name = role, NormalizedName = role, ConcurrencyStamp = role });
            }

            var Admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };
            var result = await _userManager.CreateAsync(Admin, pass);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(Admin, PMRolesConst.APP.Admin);
            }
            return false;
        }
        public async Task<IEnumerable<UserAuthModel>> GetUsersAsync(int? TenantId, int? DepartmentId)
        {
            var users = _appContext.Users.AsQueryable();
            if (TenantId.HasValue && TenantId > 0)
            {
                users = users.Where(x => x.TenantId == TenantId);
                if (DepartmentId.HasValue && DepartmentId > 0)
                    users = users.Where(x => x.DepartmentId == DepartmentId);
                else users = users.Where(x => x.DepartmentId == null);
            }
            else users = users.Where(x => !x.TenantId.HasValue);
            IList<UserAuthModel> model = [];
            foreach (var user in await users.ToListAsync())
            {
                model.Add(new UserAuthModel()
                {
                    Firstname = user.Firstname ?? string.Empty,
                    Lastname = user.Lastname ?? string.Empty,
                    LockoutStart = user.LockoutStart,
                    Email = user.Email ?? string.Empty,
                    UserId = user.UserId,
                    DepartmentId = user.DepartmentId,
                    Username = user.UserName ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    Id = user.Id,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    NormalizedEmail = user.NormalizedEmail ?? string.Empty,
                    //Roles = await _userManager.GetRolesAsync(user)
                });
            }

            return model;
        }
    }
}
