using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthPermissions.Context;

/// <summary>
/// يسهل إنشاء Migrations لـ ApplicationDbContext بدون الحاجة لتشغيل السيرفر.
/// يستخدم أول قيمة متاحة من env vars: AuthPermissionsDB ثم CATALOG_CONN ثم AuthPermissions ثم DefaultConnection.
/// </summary>
public sealed class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("AuthPermissionsDB")
                   ?? Environment.GetEnvironmentVariable("CATALOG_CONN")
                   ?? Environment.GetEnvironmentVariable("AuthPermissions")
                   ?? Environment.GetEnvironmentVariable("DefaultConnection")
                   ?? @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=SiSTOfotoAppPM2;Integrated Security=True;MultipleActiveResultSets=True";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(conn)
            .Options;

        return new ApplicationDbContext(options);
    }
}
