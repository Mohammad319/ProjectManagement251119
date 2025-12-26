using Persistence.Context;
using Persistence.Factory;
using Persistence.Interceptors;
using ProjectManagement.Services;

namespace ProjectManagement.Factories;

public sealed class DbContextFactory(TenantContext tenantContext,ITenantConnectionStringProvider connProvider,
    ITenantDbContextOptionsCache optionsCache,TenantAuditSaveChangesInterceptor interceptor,
    ITenantContextResolver resolver) : IDbContextFactoryTenant
{
    public async Task<ShardingSingleDbContext> CreateDbContextAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId <= 0)
            await resolver.EnsureResolvedAsync(ct);

        if (tenantContext.TenantId <= 0)
            throw new UnauthorizedAccessException("TenantId is not set for this request.");

        var conn = await connProvider.GetAsync(tenantContext.TenantId, ct);
        var options = optionsCache.GetOrCreate(tenantContext.TenantId, conn, interceptor);

        var db = new ShardingSingleDbContext(options)
        {
            TenantId = tenantContext.TenantId,
            CurrentUserId = tenantContext.UserId,
        };

        return db;
    }

    public ShardingSingleDbContext CreateDbContext()
    => Task.Run(() => CreateDbContextAsync(CancellationToken.None)).GetAwaiter().GetResult();
}
