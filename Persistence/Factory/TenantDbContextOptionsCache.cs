using Microsoft.Extensions.Caching.Memory;
using Persistence.Interceptors;
using System.Security.Cryptography;
using System.Text;
namespace Persistence.Factory;

public sealed class TenantDbContextOptionsCache(IMemoryCache cache) : ITenantDbContextOptionsCache
{
    public DbContextOptions<ShardingSingleDbContext> GetOrCreate(
        int tenantId,
        string connectionString,
        TenantAuditSaveChangesInterceptor interceptor)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is empty.", nameof(connectionString));

        // ✅ Cache BASE options only (no interceptors)
        var baseKey = BuildKey(tenantId, connectionString);

        var baseOptions = cache.GetOrCreate(baseKey, entry =>
        {
            // نفس سياسة الكاش التي عندك تقريبًا (تقدر تعدلها لاحقًا حسب رغبتك)
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);

            var builder = new DbContextOptionsBuilder<ShardingSingleDbContext>();

            builder.UseSqlServer(connectionString, sql =>
            {
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                sql.EnableRetryOnFailure();
            });

            // حافظنا على نفس الروح الموجودة عندك
            builder.EnableDetailedErrors(false);
            builder.EnableSensitiveDataLogging(false);

            return builder.Options;
        })!;

        // ✅ Per-call clone + add the scoped interceptor (safe)
        var finalBuilder = new DbContextOptionsBuilder<ShardingSingleDbContext>(baseOptions);
        finalBuilder.AddInterceptors(interceptor);

        return finalBuilder.Options;
    }

    private static string BuildKey(int tenantId, string connectionString)
        => $"TenantDbOptions:{tenantId}:{Sha256Short(connectionString)}";

    private static string Sha256Short(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        // 12 bytes => 24 hex chars (مفتاح صغير وكافي)
        return Convert.ToHexString(bytes, 0, 12);
    }
}
