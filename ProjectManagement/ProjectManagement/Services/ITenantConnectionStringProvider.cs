namespace ProjectManagement.Services
{
    using AuthPermissions.Context;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    public interface ITenantConnectionStringProvider
    {
        Task<string> GetAsync(int tenantId, CancellationToken ct = default);
    }

    public sealed class TenantConnectionStringProvider(
    ApplicationDbContext catalogDb,
    IMemoryCache cache) : ITenantConnectionStringProvider
    {
        public async Task<string> GetAsync(int tenantId, CancellationToken ct = default)
        {
            var conn = await cache.GetOrCreateAsync($"TenantConn:{tenantId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                entry.SlidingExpiration = TimeSpan.FromMinutes(20);

                return await catalogDb.Tenants
                    .Where(t => t.Id == tenantId && t.TenantDB != null)
                    .Select(t => t.TenantDB!.ConnectionString)
                    .FirstOrDefaultAsync(ct);
            });

            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException($"No connection string found for tenant {tenantId}.");

            return conn;
        }
    }
}
