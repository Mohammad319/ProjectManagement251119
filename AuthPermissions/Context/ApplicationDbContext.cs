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
        public DbSet<TenantMlSettingEntity> TenantMlSettings => Set<TenantMlSettingEntity>();
        public DbSet<TenantMlTrainingRunEntity> TenantMlTrainingRuns => Set<TenantMlTrainingRunEntity>();

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
                entity.Property(x => x.RefreshToken).HasMaxLength(256);
                entity.Property(x => x.Firstname).HasMaxLength(100);
                entity.Property(x => x.Lastname).HasMaxLength(100);

                entity.HasIndex(x => new { x.TenantId, x.DepartmentId })
                    .HasDatabaseName("IX_AspNetUsers_Tenant_Department");

                entity.HasIndex(x => new { x.TenantId, x.UserId })
                    .HasDatabaseName("IX_AspNetUsers_Tenant_UserId");
            });

            modelBuilder.Entity<TenantDatabaseEntity>(entity =>
            {
                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.ConnectionString)
                    .IsRequired()
                    .HasMaxLength(4000);

                entity.Property(x => x.RowVersion).IsRowVersion();

                entity.HasIndex(x => x.Name)
                    .IsUnique()
                    .HasDatabaseName("UX_TenantDatabase_Name");

                entity.ToTable("TenantDatabase", t =>
                {
                    t.HasCheckConstraint("CK_TenantDatabase_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    t.HasCheckConstraint("CK_TenantDatabase_ConnectionString_NotEmpty", "LEN(LTRIM(RTRIM([ConnectionString]))) > 0");
                });
            });

            modelBuilder.Entity<TenantEntity>(entity =>
            {
                entity.Property(x => x.Phone).HasMaxLength(30);
                entity.Property(x => x.Mobile).HasMaxLength(30);
                entity.Property(x => x.Fax).HasMaxLength(30);
                entity.Property(x => x.Email).HasMaxLength(256);
                entity.Property(x => x.Website).HasMaxLength(512);
                entity.Property(x => x.Note).HasMaxLength(1000);
                entity.Property(x => x.Country).HasMaxLength(100);
                entity.Property(x => x.City).HasMaxLength(100);
                entity.Property(x => x.PostCode).HasMaxLength(20);
                entity.Property(x => x.Street).HasMaxLength(200);
                entity.Property(x => x.BuildNumber).HasMaxLength(20);

                entity.HasIndex(x => new { x.TenantDBId, x.Name })
                    .HasDatabaseName("IX_Tenants_TenantDB_Name");

                entity.HasOne(x => x.TenantDB)
                    .WithMany(x => x.Tenants)
                    .HasForeignKey(x => x.TenantDBId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable("Tenants", t =>
                {
                    t.HasCheckConstraint("CK_Tenants_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    t.HasCheckConstraint("CK_Tenants_MaxUsers_Positive", "[MaxUsers] >= 1");
                    t.HasCheckConstraint("CK_Tenants_MaxCalculations_Positive", "[MaxCalculations] >= 1");
                });
            });

            modelBuilder.Entity<TenantMlSettingEntity>(entity =>
            {
                entity.ToTable("TenantMlSettings", t =>
                {
                    t.HasCheckConstraint("CK_TenantMlSettings_AutoTrainingIntervalDays_Min", "[AutoTrainingIntervalDays] >= 1");
                });

                entity.HasIndex(x => x.TenantId)
                    .IsUnique()
                    .HasDatabaseName("UX_TenantMlSettings_TenantId");

                entity.Property(x => x.AutoTrainingIntervalDays)
                    .HasDefaultValue(14);

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TenantMlTrainingRunEntity>(entity =>
            {
                entity.ToTable("TenantMlTrainingRuns");

                entity.Property(x => x.ModelPath)
                    .HasMaxLength(4000);

                entity.Property(x => x.Message)
                    .HasMaxLength(1000);

                entity.Property(x => x.BetterModel)
                    .HasMaxLength(50);

                entity.HasIndex(x => new { x.TenantId, x.StartedAtUtc })
                    .HasDatabaseName("IX_TenantMlTrainingRuns_Tenant_Started");

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
