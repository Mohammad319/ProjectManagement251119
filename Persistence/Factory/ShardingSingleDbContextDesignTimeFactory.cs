using Microsoft.EntityFrameworkCore.Design;

namespace Persistence.Factory
{
    public class ShardingSingleDbContextDesignTimeFactory
        : IDesignTimeDbContextFactory<ShardingSingleDbContext>
    {
        public ShardingSingleDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ShardingSingleDbContext>();

            // ضع ConnectionString ثابتة للتصميم (مثلاً dev database)
            optionsBuilder.UseSqlServer(
                @"Server=(localdb)\MSSQLLocalDB;Database=db922357028_2;Trusted_Connection=True;TrustServerCertificate=True;");

            var db = new ShardingSingleDbContext(optionsBuilder.Options)
            {
                TenantId = 0,        // قيمة افتراضية للفلتر
                CurrentUserId = null
            };

            return db;
        }
    }
}
