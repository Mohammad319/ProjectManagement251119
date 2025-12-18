using Microsoft.EntityFrameworkCore.Design;

namespace Persistence.Factory
{
    public class ShardingSingleDbContextDesignTimeFactory
        : IDesignTimeDbContextFactory<ShardingSingleDbContext>
    {
        public ShardingSingleDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ShardingSingleDbContext>();
            optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=db962510648_2;Trusted_Connection=True;TrustServerCertificate=True;");

            var db = new ShardingSingleDbContext(optionsBuilder.Options)
            {
                TenantId = 0,
                CurrentUserId = null
            };

            return db;
        }
    }
}
