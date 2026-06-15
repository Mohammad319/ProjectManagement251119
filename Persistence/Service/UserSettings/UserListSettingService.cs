using Application.Feature.General.ListSettings;
using Domain.Entities.Users;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.UserSettings;

namespace Persistence.Service.UserSettings
{
    public sealed class UserListSettingService(IDbContextFactoryTenant dbFactory) : IUserListSettingService
    {
        public async Task<List<UserListSettingDTO>> GetByScopeAsync(int userId, string scope, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // Tenant scoping is applied automatically by the global query filter.
            return await context.UserListSettings
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.Scope == scope)
                .Select(x => new UserListSettingDTO
                {
                    Scope = x.Scope,
                    Kind = x.Kind,
                    Payload = x.Payload
                })
                .ToListAsync(ct);
        }

        public async Task UpsertAsync(int userId, string scope, string kind, string payload, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var existing = await context.UserListSettings
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Scope == scope && x.Kind == kind, ct);

            if (existing is null)
            {
                context.UserListSettings.Add(UserListSettingEntity.Create(userId, scope, kind, payload));
            }
            else
            {
                existing.SetPayload(payload);
            }

            await context.SaveChangesAsync(ct);
        }
    }
}
