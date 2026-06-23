using Application.Interfaces;
using AuthPermissions.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Services
{
    /// <summary>
    /// Reads tenant roles from the identity store (<see cref="AuthPermissionDbContext"/>) via a fresh DI
    /// scope – never the circuit-scoped context – mirroring <see cref="UI.DepartmentUsersViewService"/>.
    /// </summary>
    public sealed class UserSystemRoleProvider(IServiceScopeFactory scopeFactory) : IUserSystemRoleProvider
    {
        public async Task<IReadOnlySet<string>> GetViewerAuthIdsAsync(IEnumerable<string> externalAuthIds, CancellationToken ct = default)
        {
            var ids = externalAuthIds
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (ids.Count == 0)
                return new HashSet<string>(StringComparer.Ordinal);

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthPermissionDbContext>();

            var viewerRole = PMRolesConst.Tenant.Viewer;

            var viewerAuthIds = await (
                    from ur in db.UserRoles.AsNoTracking()
                    join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                    where ids.Contains(ur.UserId) && r.Name == viewerRole
                    select ur.UserId)
                .Distinct()
                .ToListAsync(ct);

            return viewerAuthIds.ToHashSet(StringComparer.Ordinal);
        }
    }
}
