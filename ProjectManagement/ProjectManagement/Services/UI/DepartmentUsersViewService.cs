using AuthPermissions.Context;
using Domain.DTO.User;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Services;

namespace ProjectManagement.Services.UI;

public interface IDepartmentUsersViewService
{
    Task<List<TenantUserDto>> GetUsersAsync(int? departmentId, CancellationToken ct = default);
}

/// <summary>
/// UI facade for department user management screens.
/// It consolidates local tenant users with Identity/auth state so the component
/// does not need to know how the data is assembled.
/// </summary>
public sealed class DepartmentUsersViewService(
    ITenantUserService tenantUserService,
    UserManager<ApplicationUser> userManager,
    ITenantContext tenantContext) : IDepartmentUsersViewService
{
    public async Task<List<TenantUserDto>> GetUsersAsync(int? departmentId, CancellationToken ct = default)
    {
        var users = await tenantUserService.GetAllTenantUsersAsync(departmentId, ct);

        var authUsers = await userManager.Users
            .AsNoTracking()
            .Where(x => x.TenantId == tenantContext.TenantId)
            .Select(x => new
            {
                x.Id,
                x.UserName,
                x.Email,
                x.LockoutEnabled,
                x.LockoutStart,
                x.LockoutEnd
            })
            .ToListAsync(ct);

        var authLookup = authUsers.ToDictionary(x => x.Id, StringComparer.Ordinal);

        foreach (var user in users)
        {
            if (!string.IsNullOrWhiteSpace(user.IdAuth) && authLookup.TryGetValue(user.IdAuth, out var auth))
            {
                user.IsInAuth = true;
                user.Username ??= auth.UserName;
                user.Email ??= auth.Email;
                user.LockoutEnabled = auth.LockoutEnabled;
                user.LockoutStart = auth.LockoutStart;
                user.LockoutEnd = auth.LockoutEnd;
            }
            else
            {
                user.IsInAuth = false;
            }
        }

        // Roles are resolved only for auth users. This is an admin/settings screen,
        // so the small per-user lookup cost is acceptable and keeps the component clean.
        foreach (var user in users.Where(x => x.IsInAuth && !string.IsNullOrWhiteSpace(x.IdAuth)))
        {
            var authUser = await userManager.FindByIdAsync(user.IdAuth!);
            if (authUser is null)
                continue;

            var roles = await userManager.GetRolesAsync(authUser);
            user.Role = roles.FirstOrDefault() ?? string.Empty;
        }

        return users
            .OrderByDescending(x => x.IsInAuth)
            .ThenBy(x => x.Firstname)
            .ThenBy(x => x.Lastname)
            .ThenBy(x => x.Email)
            .ToList();
    }
}
