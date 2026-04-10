using AuthPermissions.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.Context
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        protected override Version SchemaVersion => IdentitySchemaVersions.Version3;

        public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
        public DbSet<LogEntity> Logs => Set<LogEntity>();
        public DbSet<TenantDatabaseEntity> TenantDatabase => Set<TenantDatabaseEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LogEntity>(entity =>
            {
                entity.ToTable("Logs", table => table.ExcludeFromMigrations());

                // Serilog owns this table and several text columns can be NULL.
                entity.Property(x => x.Message).IsRequired(false);
                entity.Property(x => x.MessageTemplate).IsRequired(false);
                entity.Property(x => x.Level).IsRequired(false);
                entity.Property(x => x.Exception).IsRequired(false);
                entity.Property(x => x.Properties).IsRequired(false);
                entity.Property(x => x.UserId).HasMaxLength(450).IsRequired(false);
            });

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(x => x.DB).HasMaxLength(128);
                entity.Property(x => x.RefreshToken).HasMaxLength(50);
                entity.Property(x => x.Firstname).HasMaxLength(100);
                entity.Property(x => x.Lastname).HasMaxLength(100);

                entity.HasIndex(x => new { x.TenantId, x.DepartmentId })
                    .HasDatabaseName("IX_AspNetUsers_Tenant_Department");

                entity.HasIndex(x => new { x.TenantId, x.UserId })
                    .HasDatabaseName("IX_AspNetUsers_Tenant_UserId");
            });

            modelBuilder.Entity<TenantDatabaseEntity>(entity =>
            {
                entity.ToTable("TenantDatabase");

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.ConnectionString)
                    .IsRequired()
                    .HasMaxLength(4000);

                entity.HasIndex(x => x.Name)
                    .IsUnique()
                    .HasDatabaseName("UX_TenantDatabase_Name");
            });

            modelBuilder.Entity<TenantEntity>(entity =>
            {
                entity.HasIndex(x => new { x.TenantDBId, x.Name })
                    .HasDatabaseName("IX_Tenants_TenantDB_Name");

                entity.HasOne(x => x.TenantDB)
                    .WithMany(x => x.Tenants)
                    .HasForeignKey(x => x.TenantDBId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
