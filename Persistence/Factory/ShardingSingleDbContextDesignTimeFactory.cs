using Microsoft.EntityFrameworkCore.Design;

namespace Persistence.Factory
{
    public sealed class ShardingSingleDesignTimeFactory : IDesignTimeDbContextFactory<ShardingSingleDbContext>
    {
        public ShardingSingleDbContext CreateDbContext(string[] args)
        {
            var conn = Environment.GetEnvironmentVariable("TENANT_TEMPLATE_CONN")
                       ?? "Server=.;Database=TenantTemplateDb;Trusted_Connection=True;TrustServerCertificate=True;";

            var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
                .UseSqlServer(conn)
                .Options;

            return new ShardingSingleDbContext(options) { TenantId = 1 };
        }
    }

}
