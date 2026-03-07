using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Persistence.Factory
{
    public sealed class ShardingSingleDesignTimeFactory : IDesignTimeDbContextFactory<ShardingSingleDbContext>
    {
        public ShardingSingleDbContext CreateDbContext(string[] args)
        {
            var conn = FirstNonEmpty(
                           Environment.GetEnvironmentVariable("PM_TEMPLATE_CONN"),
                           Environment.GetEnvironmentVariable("TENANT_TEMPLATE_CONN"))
                       ?? @"Data Source=.\SQLEXPRESS;Initial Catalog=ProjectManagement251119;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=True;Application Intent=ReadWrite;MultipleActiveResultSets=True";

            var tenantId = 1;
            var tenantRaw = FirstNonEmpty(
                Environment.GetEnvironmentVariable("PM_DESIGNTIME_TENANT_ID"),
                Environment.GetEnvironmentVariable("TENANT_ID"));

            if (!string.IsNullOrWhiteSpace(tenantRaw) && int.TryParse(tenantRaw, out var parsedTenantId) && parsedTenantId > 0)
                tenantId = parsedTenantId;

            var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
                .UseSqlServer(conn, sql => sql.EnableRetryOnFailure())
                .Options;

            return new ShardingSingleDbContext(options)
            {
                TenantId = tenantId
            };
        }

        private static string? FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }
}
