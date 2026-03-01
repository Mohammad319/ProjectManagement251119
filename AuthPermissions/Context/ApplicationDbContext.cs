using AuthPermissions.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.Context
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<TenantEntity> Tenants { get; set; }
        public DbSet<LogEntity> Logs { get; set; }
        public DbSet<TenantDatabaseEntity> TenantDatabase { get; set; }

        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Serilog creates Logs table (autoCreateSqlTable=true) => keep it out of EF migrations
            modelBuilder.Entity<LogEntity>()
                .ToTable("Logs", table => table.ExcludeFromMigrations());

            // ---- Tenants catalog ----
            modelBuilder.Entity<TenantDatabaseEntity>(b =>
            {
                b.ToTable("TenantDatabases");

                b.Property(x => x.Name)
                    .HasMaxLength(80)
                    .IsRequired();

                b.Property(x => x.ConnectionString)
                    .HasMaxLength(1000)
                    .IsRequired();

                // منع التكرار
                b.HasIndex(x => x.Name).IsUnique();
                b.HasIndex(x => x.ConnectionString).IsUnique();
            });

            modelBuilder.Entity<TenantEntity>(b =>
            {
                b.ToTable("Tenants");

                // لا نريد تينانتين بنفس الاسم داخل نفس Catalog
                b.Property(x => x.Name)
                    .HasMaxLength(80)
                    .IsRequired();

                b.HasIndex(x => x.Name).IsUnique();

                b.HasOne(x => x.TenantDB)
                    .WithMany(x => x.Tenants)
                    .HasForeignKey(x => x.TenantDBId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
