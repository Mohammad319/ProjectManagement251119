using Persistence.Context;
using Persistence.Factory;
using Persistence.Interceptors;

namespace ProjectManagement.Services;

public sealed class DbContextFactory(
    TenantContext tenantContext,
    ITenantConnectionStringStore store,
    ITenantDbContextFactoryCache factoryCache,
    TenantAuditSaveChangesInterceptor interceptor,
    ITenantContextResolver resolver) : IDbContextFactoryTenant
{
    public async Task<ShardingSingleDbContext> CreateDbContextAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId <= 0)
            await resolver.EnsureResolvedAsync(ct).ConfigureAwait(false);

        if (tenantContext.TenantId <= 0)
            throw new UnauthorizedAccessException("TenantId is not set for this request.");

        if (!store.TryGet(tenantContext.TenantId, out var conn) || string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException($"No connection string found for TenantId={tenantContext.TenantId}.");

        var factory = factoryCache.GetOrCreate(tenantContext.TenantId, conn, interceptor);

        var db = await factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        db.TenantId = tenantContext.TenantId;
        db.CurrentUserId = tenantContext.UserId;
        return db;
    }
}
