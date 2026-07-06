using AuthPermissions.Context;
using Domain.DTO.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Services;

namespace ProjectManagement.Services.UI;

public interface IDepartmentUsersViewService
{
    Task<List<TenantUserDto>> GetUsersAsync(int? departmentId, bool withoutDepartmentOnly = false, CancellationToken ct = default);

    /// <summary>The current tenant's licensed maximum number of users (0 when unknown/unlimited).</summary>
    Task<int> GetTenantMaxUsersAsync(CancellationToken ct = default);
}

/// <summary>
/// UI facade for department user management screens.
/// It consolidates local tenant users with Identity/auth state so the component
/// does not need to know how the data is assembled.
/// </summary>
/// <remarks>
/// Auth data (lockout state + roles) is read through a dedicated <see cref="AuthPermissionDbContext"/>
/// resolved from a fresh DI scope — never through the circuit-scoped <c>UserManager</c>. Sharing that
/// scoped context with the authentication-state provider can raise "a second operation was started on
/// this context" and tear down the Blazor circuit. Roles are also fetched in a single batched query
/// instead of a per-user round-trip, so listing "all users" stays cheap.
/// </remarks>
public sealed class DepartmentUsersViewService(
    ITenantUserService tenantUserService,
    IServiceScopeFactory scopeFactory,
    ITenantContext tenantContext) : IDepartmentUsersViewService
{
    public async Task<List<TenantUserDto>> GetUsersAsync(int? departmentId, bool withoutDepartmentOnly = false, CancellationToken ct = default)
    {
        var users = await tenantUserService.GetAllTenantUsersAsync(departmentId, ct);
        if (withoutDepartmentOnly && !departmentId.HasValue)
            users = users.Where(x => x.DepartmentIds.Count == 0 && !x.DepartmentId.HasValue).ToList();

        var authIds = users
            .Select(x => x.IdAuth)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (authIds.Count == 0)
            return OrderUsers(users);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthPermissionDbContext>();

        var authUsers = await db.Users
            .AsNoTracking()
            .Where(x => x.TenantId == tenantContext.TenantId && authIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.UserName,
                x.Email,
                x.LockoutEnabled,
                x.LockoutStart,
                x.LockoutEnd,
                x.LastLoginAt,
                x.IsActive,
                x.PhoneNumber,
                x.PhoneNumberConfirmed
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
                user.LastLoginAt = auth.LastLoginAt;
                user.IsActive = auth.IsActive;
                user.PhoneNumber = auth.PhoneNumber;
                user.PhoneNumberConfirmed = auth.PhoneNumberConfirmed;
            }
            else
            {
                user.IsInAuth = false;
            }
        }

        // Resolve roles for all auth users in a single query (UserRoles ⋈ Roles) instead of a
        // per-user UserManager round-trip, which scales linearly and previously hit the circuit DbContext.
        var existingAuthIds = users
            .Where(x => x.IsInAuth && !string.IsNullOrWhiteSpace(x.IdAuth))
            .Select(x => x.IdAuth!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (existingAuthIds.Count > 0)
        {
            var roleNameByUser = await (
                    from ur in db.UserRoles.AsNoTracking()
                    join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                    where existingAuthIds.Contains(ur.UserId)
                    select new { ur.UserId, r.Name })
                .ToListAsync(ct);

            var firstRole = roleNameByUser
                .GroupBy(x => x.UserId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First().Name ?? string.Empty, StringComparer.Ordinal);

            foreach (var user in users.Where(x => x.IsInAuth && !string.IsNullOrWhiteSpace(x.IdAuth)))
                user.Role = firstRole.TryGetValue(user.IdAuth!, out var roleName) ? roleName : string.Empty;
        }

        return OrderUsers(users);
    }

    public async Task<int> GetTenantMaxUsersAsync(CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthPermissionDbContext>();

        return await db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantContext.TenantId)
            .Select(t => t.MaxUsers)
            .FirstOrDefaultAsync(ct);
    }

    private static List<TenantUserDto> OrderUsers(IEnumerable<TenantUserDto> users)
        => users
            .OrderByDescending(x => x.IsInAuth)
            .ThenBy(x => x.Firstname)
            .ThenBy(x => x.Lastname)
            .ThenBy(x => x.Email)
            .ToList();
}
