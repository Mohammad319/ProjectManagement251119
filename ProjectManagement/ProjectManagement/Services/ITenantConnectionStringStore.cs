using AuthPermissions.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace ProjectManagement.Services
{
    public interface ITenantConnectionStringStore
    {
        bool TryGet(int tenantId, out string cs);
        Task ReloadAsync(CancellationToken ct = default);
        Task ReloadTenantAsync(int tenantId, CancellationToken ct = default);
    }

    public sealed class TenantConnectionStringStore(IServiceScopeFactory scopeFactory) : ITenantConnectionStringStore
    {
        private readonly ConcurrentDictionary<int, string> _map = new();

        public bool TryGet(int tenantId, out string connectionString)
            => _map.TryGetValue(tenantId, out connectionString!);

        public async Task ReloadAsync(CancellationToken ct = default)
        {
            using var scope = scopeFactory.CreateScope();
            var catalogDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var rows = await catalogDb.Tenants
                .AsNoTracking()
                .Where(t => t.TenantDB != null && !string.IsNullOrWhiteSpace(t.TenantDB!.ConnectionString))
                .Select(t => new { t.Id, t.TenantDB!.ConnectionString })
                .ToListAsync(ct);

            _map.Clear();
            foreach (var row in rows)
            {
                _map[row.Id] = row.ConnectionString;
            }
        }

        public async Task ReloadTenantAsync(int tenantId, CancellationToken ct = default)
        {
            using var scope = scopeFactory.CreateScope();
            var catalogDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var row = await catalogDb.Tenants
                .AsNoTracking()
                .Where(t => t.Id == tenantId && t.TenantDB != null)
                .Select(t => t.TenantDB!.ConnectionString)
                .SingleOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(row))
            {
                _map.TryRemove(tenantId, out _);
                return;
            }

            _map[tenantId] = row;
        }
    }
}
