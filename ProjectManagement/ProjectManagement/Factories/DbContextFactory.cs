using Persistence.Context;
using Persistence.Factory;
using Persistence.Interceptors;
using ProjectManagement.Services;

namespace ProjectManagement.Factories;

public sealed class DbContextFactory(
    TenantContext tenantContext,
    ITenantConnectionStringProvider connProvider,
    ITenantDbContextOptionsCache optionsCache,
    TenantAuditSaveChangesInterceptor interceptor,
    ITenantContextResolver resolver) : IDbContextFactoryTenant
{
    public async Task<ShardingSingleDbContext> CreateDbContextAsync(CancellationToken ct = default)
    {
        // 1) Ensure Tenant is resolved (HTTP Middleware / HubFilter / CircuitHandler)
        if (tenantContext.TenantId <= 0)
            await resolver.EnsureResolvedAsync(ct).ConfigureAwait(false);

        if (tenantContext.TenantId <= 0)
            throw new UnauthorizedAccessException("TenantId is not set for this request.");

        // 2) Get connection string per tenant
        var conn = await connProvider.GetAsync(tenantContext.TenantId, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException($"No connection string found for TenantId={tenantContext.TenantId}.");

        // 3) Get cached options per tenant (IMPORTANT for perf)
        var options = optionsCache.GetOrCreate(tenantContext.TenantId, conn, interceptor);

        // 4) Create DbContext
        var db = new ShardingSingleDbContext(options)
        {
            TenantId = tenantContext.TenantId,
            CurrentUserId = tenantContext.UserId,
        };

        return db;
    }
}
