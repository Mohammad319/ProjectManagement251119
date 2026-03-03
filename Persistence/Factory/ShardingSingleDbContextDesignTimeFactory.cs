using Microsoft.EntityFrameworkCore.Design;

namespace Persistence.Factory
{
    public sealed class ShardingSingleDesignTimeFactory : IDesignTimeDbContextFactory<ShardingSingleDbContext>
    {
        public ShardingSingleDbContext CreateDbContext(string[] args)
        {
            var conn = Environment.GetEnvironmentVariable("TENANT_TEMPLATE_CONN")
                       ?? @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=PM_Tenant_DB2;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False;MultipleActiveResultSets=True";

            var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
                .UseSqlServer(conn).Options;

            return new ShardingSingleDbContext(options) { TenantId = 1 };
        }
    }

}
