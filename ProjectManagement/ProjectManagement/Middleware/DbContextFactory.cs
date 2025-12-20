using Persistence.Context;
using Persistence.Factory;
using Persistence.Interceptors;
using ProjectManagement.Services;

namespace ProjectManagement.Middleware;

public sealed class DbContextFactory(
    TenantContext tenantContext,
    ITenantConnectionStringProvider connProvider,
    ITenantDbContextOptionsCache optionsCache,
    TenantAuditSaveChangesInterceptor interceptor,
    ITenantContextResolver resolver)
    : IDbContextFactory
{
    public async Task<ShardingSingleDbContext> CreateDbContextAsync(CancellationToken ct = default)
    {
        // ✅ في Blazor Server circuit غالبًا TenantId لا يُعبّى بالميدلوير
        if (tenantContext.TenantId <= 0)
            await resolver.EnsureResolvedAsync(ct);

        if (tenantContext.TenantId <= 0)
            throw new UnauthorizedAccessException("TenantId is not set for this request.");

        var conn = await connProvider.GetAsync(tenantContext.TenantId, ct);
        var options = optionsCache.GetOrCreate(tenantContext.TenantId, conn, interceptor);

        return new ShardingSingleDbContext(options)
        {
            TenantId = tenantContext.TenantId,
            CurrentUserId = tenantContext.UserId,
        };
    }

    public ShardingSingleDbContext CreateDbContext()
        => CreateDbContextAsync().GetAwaiter().GetResult();
}
