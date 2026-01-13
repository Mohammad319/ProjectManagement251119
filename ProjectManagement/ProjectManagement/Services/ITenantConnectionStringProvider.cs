namespace ProjectManagement.Services
{
    public interface ITenantConnectionStringProvider
    {
        Task<string> GetAsync(int tenantId, CancellationToken ct = default);
    }
    public sealed class TenantConnectionStringProvider(ITenantConnectionStringStore store)
        : ITenantConnectionStringProvider
    {
        public Task<string> GetAsync(int tenantId, CancellationToken ct = default)
        {
            if (!store.TryGet(tenantId, out var conn) || string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException($"No connection string found for tenant {tenantId}.");

            return Task.FromResult(conn);
        }
    }
}
