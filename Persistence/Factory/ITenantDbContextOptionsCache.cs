using Persistence.Interceptors;

namespace Persistence.Factory;

public interface ITenantDbContextOptionsCache
{
    DbContextOptions<ShardingSingleDbContext> GetOrCreate(
        int tenantId,
        string connectionString,
        TenantAuditSaveChangesInterceptor interceptor);
}
