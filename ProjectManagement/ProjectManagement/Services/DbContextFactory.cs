using Microsoft.Extensions.Logging;
using Persistence.Context;
using Persistence.Factory;
using Persistence.Interceptors;

namespace ProjectManagement.Services;

public sealed class DbContextFactory(
    TenantContext tenantContext,
    ITenantConnectionStringStore store,
    ITenantDbContextFactoryCache factoryCache,
    TenantAuditSaveChangesInterceptor interceptor,
    ITenantContextResolver resolver,
    ILogger<DbContextFactory> logger) : IDbContextFactoryTenant
{
    public async Task<ShardingSingleDbContext> CreateDbContextAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId <= 0)
            await resolver.EnsureResolvedAsync(ct).ConfigureAwait(false);

        if (tenantContext.TenantId <= 0)
        {
            logger.LogWarning("Tenant DbContext creation was requested without a resolved TenantId.");
            throw new UnauthorizedAccessException("TenantId is not set for this request.");
        }

        if (!store.TryGet(tenantContext.TenantId, out var conn) || string.IsNullOrWhiteSpace(conn))
        {
            logger.LogWarning(
                "Tenant connection string was not found in cache for TenantId={TenantId}. Reloading tenant mapping from AuthPermissions.",
                tenantContext.TenantId);

            await store.ReloadTenantAsync(tenantContext.TenantId, ct).ConfigureAwait(false);
        }

        if (!store.TryGet(tenantContext.TenantId, out conn) || string.IsNullOrWhiteSpace(conn))
        {
            logger.LogError(
                "No tenant connection string could be resolved for TenantId={TenantId} after cache reload.",
                tenantContext.TenantId);

            throw new InvalidOperationException($"No connection string found for TenantId={tenantContext.TenantId}.");
        }

        var factory = factoryCache.GetOrCreate(tenantContext.TenantId, conn, interceptor);

        var db = await factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        db.TenantId = tenantContext.TenantId;
        db.CurrentUserId = tenantContext.UserId;
        return db;
    }
}
