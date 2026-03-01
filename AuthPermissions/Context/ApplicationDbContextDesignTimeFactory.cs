using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthPermissions.Context;

/// <summary>
/// يسهل إنشاء Migrations لـ ApplicationDbContext بدون الحاجة لتشغيل السيرفر.
/// استخدم env var: CATALOG_CONN (أو عدّل الـ fallback حسب مشروعك).
/// </summary>
public sealed class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("CATALOG_CONN")
                   ?? @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ProjectManagement_Catalog;Integrated Security=True;MultipleActiveResultSets=True";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(conn)
            .Options;

        return new ApplicationDbContext(options);
    }
}
