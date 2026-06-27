using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Persistence.Context;
using Persistence.Interceptors;
using System.Security.Cryptography;
using System.Text;

namespace Persistence.Factory;

/// <summary>
/// Caches a pooled EF Core DbContext factory per tenant + connection string.
/// This drastically reduces allocations/GC and improves request throughput.
/// </summary>
public interface ITenantDbContextFactoryCache
{
    IDbContextFactory<ShardingSingleDbContext> GetOrCreate(
        int tenantId,
        string connectionString,
        TenantAuditSaveChangesInterceptor auditInterceptor);
}

public sealed class TenantDbContextFactoryCache(IMemoryCache cache) : ITenantDbContextFactoryCache
{
    public IDbContextFactory<ShardingSingleDbContext> GetOrCreate(
        int tenantId,
        string connectionString,
        TenantAuditSaveChangesInterceptor auditInterceptor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is empty.", nameof(connectionString));

        var key = BuildKey(tenantId, connectionString);

        return cache.GetOrCreate(key, entry =>
        {
            // تنتهي الصلاحية بعد 24 ساعة على الأقل أو 4 ساعات من عدم الاستخدام
            // هذا يحمي من memory leak في سيناريوهات multi-tenant وتغيير connection strings
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            entry.SlidingExpiration = TimeSpan.FromHours(4);

            var builder = new DbContextOptionsBuilder<ShardingSingleDbContext>();

            builder.UseSqlServer(connectionString, sql =>
            {
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                sql.EnableRetryOnFailure();
            });

            builder.EnableDetailedErrors(false);
            builder.EnableSensitiveDataLogging(false);

            // ✅ Share one EF model across all tenants
            builder.ReplaceService<IModelCacheKeyFactory, TenantAgnosticModelCacheKeyFactory>();

            // ✅ Interceptor is stateless in your code, safe as Singleton.
            builder.AddInterceptors(auditInterceptor);

            // Pool size: keep modest by default; tune if needed.
            const int poolSize = 256;
            return new PooledDbContextFactory<ShardingSingleDbContext>(builder.Options, poolSize);
        })!;
    }

    private static string BuildKey(int tenantId, string connectionString)
        => $"TenantDbFactory:{tenantId}:{Sha256Short(connectionString)}";

    private static string Sha256Short(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        // 12 bytes => 24 hex chars (short but collision-resistant enough for cache key)
        return Convert.ToHexString(bytes, 0, 12);
    }
}
