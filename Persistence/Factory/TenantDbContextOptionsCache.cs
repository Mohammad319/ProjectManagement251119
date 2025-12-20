using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Persistence.Context;
using Persistence.Interceptors;

namespace Persistence.Factory;

/// <summary>
/// كاش لـ DbContextOptions لكل Tenant لتقليل overhead إنشاء options builder كل مرة.
/// </summary>
public sealed class TenantDbContextOptionsCache(IMemoryCache cache) : ITenantDbContextOptionsCache
{
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6),
    };

    public DbContextOptions<ShardingSingleDbContext> GetOrCreate(
        int tenantId,
        string connectionString,
        TenantAuditSaveChangesInterceptor interceptor)
    {
        var key = $"tenant-db-options:{tenantId}:{connectionString.GetHashCode()}";

        return cache.GetOrCreate(key, entry =>
        {
            entry.SetOptions(CacheOptions);

            var builder = new DbContextOptionsBuilder<ShardingSingleDbContext>()
                .UseSqlServer(connectionString, sql =>
                {
                    sql.EnableRetryOnFailure();
                    sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
                .AddInterceptors(interceptor);

            return builder.Options;
        })!;
    }
}
