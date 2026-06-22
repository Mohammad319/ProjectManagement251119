using AuthPermissions.Context;
using Domain.Repository.AuthPermissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Models.Account;

namespace AuthPermissions.Services.Implement;

public class AuthRepository(
    AuthPermissionDbContext appContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : IAuthRepository
{
    // Tenant-scope role names; used to pick a user's tenant role (ignoring any app-scope role)
    // when projecting UserAuthModel.Role for the sharing/department UIs.
    private static readonly string[] TenantRoleNames =
    [
        PMRolesConst.Tenant.Admin,
        PMRolesConst.Tenant.Manger,
        PMRolesConst.Tenant.User,
        PMRolesConst.Tenant.Viewer
    ];

    public static string[] GetRoles() => [.. IdentityUserSyncHelper.GetAllRoles()];

    public async Task<bool> Initialize(string email, string pass)
    {
        if (!await IdentityUserSyncHelper.EnsureRolesExistAsync(roleManager, IdentityUserSyncHelper.GetAllRoles()))
            return false;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
            return true;

        var normalizedEmail = email.Trim();
        var admin = await userManager.FindByEmailAsync(normalizedEmail);
        var created = false;

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                EmailConfirmed = true,
                TenantId = null,
                DepartmentId = null
            };

            var createResult = await userManager.CreateAsync(admin, pass);
            if (!createResult.Succeeded)
                return false;

            created = true;
        }
        else
        {
            IdentityUserSyncHelper.ApplyToIdentityUser(
                admin,
                normalizedEmail,
                normalizedEmail,
                tenantId: null,
                departmentId: null,
                localUserId: null,
                firstName: admin.Firstname,
                lastName: admin.Lastname,
                phoneNumber: admin.PhoneNumber,
                phoneNumberConfirmed: admin.PhoneNumberConfirmed,
                lockoutEnabled: admin.LockoutEnabled,
                lockoutStart: admin.LockoutStart,
                lockoutEnd: admin.LockoutEnd,
                isAppUser: true);

            admin.EmailConfirmed = true;

            var updateResult = await userManager.UpdateAsync(admin);
            if (!updateResult.Succeeded)
                return false;
        }

        if (!await IdentityUserSyncHelper.EnsureExactRolesAsync(
                userManager,
                admin,
                IdentityUserSyncHelper.GetAppRoles(),
                IdentityUserSyncHelper.GetAllRoles()))
        {
            return false;
        }

        if (!created)
            await userManager.UpdateSecurityStampAsync(admin);

        return true;
    }

    public async Task<IEnumerable<UserAuthModel>> GetUsersAsync(int? tenantId, int? departmentId)
    {
        var users = appContext.Users.AsNoTracking().AsQueryable();

        if (tenantId.HasValue && tenantId > 0)
        {
            users = users.Where(x => x.TenantId == tenantId);
            if (departmentId.HasValue && departmentId > 0)
                users = users.Where(x => x.DepartmentId == departmentId);
        }
        else
        {
            users = users.Where(x => !x.TenantId.HasValue);
        }

        return await users
            .OrderBy(x => x.Email)
            .Select(user => new UserAuthModel
            {
                Firstname = user.Firstname,
                Lastname = user.Lastname,
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
                // The user's tenant role (e.g. "TenantUser"/"TenantViewer"). Needed by the project
                // sharing dialog to cap system-Visare and to summarise department access. App-scope
                // roles are intentionally excluded.
                Role = (from ur in appContext.UserRoles
                        join r in appContext.Roles on ur.RoleId equals r.Id
                        where ur.UserId == user.Id && TenantRoleNames.Contains(r.Name)
                        select r.Name).FirstOrDefault() ?? string.Empty
            })
            .ToListAsync();
    }
}
