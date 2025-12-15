using Domain.Entities.Application;
using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using Domain.Entities.Project;
using Domain.Entities.ResourceType;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Persistence.Configurations;
using ProjectManagement.Shared.Base.Application;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using ProjectManagement.Shared.DTO.Organisation;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Persistence.Context
{
    public class ShardingSingleDbContext(DbContextOptions<ShardingSingleDbContext> options)
        : DbContext(options)
    {
        /// <summary>
        /// قيمة الـ Tenant الحالية، يجب تعيينها من الطبقة الأعلى (Middleware / Service).
        /// </summary>
        public int TenantId { get; set; }
        public int? CurrentUserId { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var entityTypes = modelBuilder.Model.GetEntityTypes()
    .Where(t => typeof(IDataKeyFilterReadOnly).IsAssignableFrom(t.ClrType));

            foreach (var entityType in entityTypes)
            {
                var param = Expression.Parameter(entityType.ClrType, "e");
                var tenantProperty = Expression.Property(param, nameof(IDataKeyFilterReadOnly.TenantId));

                // this.TenantId
                var currentTenantId = Expression.Constant(TenantId);
                var body = Expression.Equal(tenantProperty, currentTenantId);

                var lambda = Expression.Lambda(body, param);

                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(lambda);
            }
            ///-------------------------------------------------------

            modelBuilder.Entity<ResourceEntity>(builder =>
            {
                builder.OwnsOne(r => r.Cost, owned =>
                {
                    owned.Property(x => x.BaseCost).HasColumnName("BaseCost");
                    owned.Property(x => x.Cost).HasColumnName("Cost");
                    owned.Property(x => x.ChangeFactor1).HasColumnName("ChangeFactor1");
                    owned.Property(x => x.ChangeFactor2).HasColumnName("ChangeFactor2");
                });
            });

            ///-------------------------------------------------------
            ConfigureTenderAttributeRelations(modelBuilder);
            ConfigureEntityConfigurations(modelBuilder);
            ConfigureJsonDataConversions(modelBuilder);
            ConfigureOrderSequences(modelBuilder);
            ConfigureGlobalTenantFilter(modelBuilder);
        }

        #region Helper configuration methods

        private static void ConfigureTenderAttributeRelations(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TenderAttributeBindEntity>()
                .HasKey(m => new { m.TenderAttributeId, m.TenderId });

            modelBuilder.Entity<TenderAttributeBindEntity>()
                .HasOne(u => u.TenderAttribute)
                .WithMany(u => u.TendersAttributes)
                .IsRequired()
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<TenderAttributeBindEntity>()
                .HasOne(pt => pt.Tender)
                .WithMany(p => p.TendersAttributes)
                .HasForeignKey(pt => pt.TenderId);
        }

        private static void ConfigureEntityConfigurations(ModelBuilder modelBuilder)
        {
            // يمكنك أيضًا استخدام:
            // modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShardingSingleDbContext).Assembly);

            modelBuilder.ApplyConfiguration(new ProjectConfiguration());
            modelBuilder.ApplyConfiguration(new FolderConfiguration());
            modelBuilder.ApplyConfiguration(new DepartmentConfiguration());

            modelBuilder.ApplyConfiguration(new CalculationConfiguration());
            modelBuilder.ApplyConfiguration(new ResourceTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ResourceSortConfiguration());
            modelBuilder.ApplyConfiguration(new ResourceConfiguration());
            modelBuilder.ApplyConfiguration(new OfferConfiguration());
            modelBuilder.ApplyConfiguration(new TaskConfiguration());
        }

        private static void ConfigureJsonDataConversions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountEntity>()
                .Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<AccountData>(v, JsonSerializerOptions.Default) ?? new AccountData());

            modelBuilder.Entity<OrganisationEntity>()
                .Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<OrganisationData>(v,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new OrganisationData());

            modelBuilder.Entity<OpportunityEntity>()
                .Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<OpportunityData>(v, JsonSerializerOptions.Default) ?? new OpportunityData());

            modelBuilder.Entity<ShareCalcEntity>()
                .Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<ShareCalcData>(v, JsonSerializerOptions.Default) ?? new ShareCalcData());

            modelBuilder.Entity<ApplicationEntity>()
                .Property(e => e.Data)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<ApplicationDataEntity>(v, JsonSerializerOptions.Default) ?? new ApplicationDataEntity());

            modelBuilder.Entity<ApplicationValuesEntity>()
                .Property(e => e.Data)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<ApplicationValuesData>(v, JsonSerializerOptions.Default) ?? new ApplicationValuesData());

            modelBuilder.Entity<TemplateEntity>()
                .Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<TemplateData>(v, JsonSerializerOptions.Default) ?? new TemplateData());
        }

        private static void ConfigureOrderSequences(ModelBuilder modelBuilder)
        {
            modelBuilder.HasSequence<int>("OrderSeq")
                        .StartsAt(0)
                        .IncrementsBy(100);

            modelBuilder.Entity<StatusEntity>()
                .Property(o => o.SortOrder)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<TaskStatusEntity>()
                .Property(o => o.SortOrder)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<StatusResourcesEntity>()
                .Property(o => o.SortOrder)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<TypeEntity>()
                .Property(o => o.SortOrder)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<ContractEntity>()
                .Property(o => o.SortOrder)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<CompensationEntity>()
                .Property(o => o.SortOrder)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<ProcurementMethodEntity>()
                .Property(o => o.SortOrder)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<ResourceTypeEntity>()
                .Property(o => o.SortOrder)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");
        }

        /// <summary>
        /// تطبيق Global Query Filter لكل الكيانات التي تطبّق IDataKeyFilterReadOnly
        /// بحيث يتم فلترتها تلقائيًا حسب TenantId الحالي في الـ DbContext.
        /// </summary>
        private void ConfigureGlobalTenantFilter(ModelBuilder modelBuilder)
        {
            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Where(t => typeof(IDataKeyFilterReadOnly).IsAssignableFrom(t.ClrType));

            var methodInfo = typeof(ShardingSingleDbContext)
                .GetMethod(nameof(SetTenantQueryFilter), BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var entityType in entityTypes)
            {
                var genericMethod = methodInfo?.MakeGenericMethod(entityType.ClrType);
                genericMethod?.Invoke(this, new object[] { modelBuilder });
            }
        }

        private void SetTenantQueryFilter<TEntity>(ModelBuilder modelBuilder)
            where TEntity : class, IDataKeyFilterReadOnly
        {
            modelBuilder.Entity<TEntity>()
                .HasQueryFilter(e => e.TenantId == TenantId);
        }

        #endregion

        #region DbSets

        public DbSet<TenderEntity> Tenders { get; set; } = default!;
        public DbSet<ProjectEntity> Projects { get; set; } = default!;
        public DbSet<FolderEntity> Folders { get; set; } = default!;
        public DbSet<StatusEntity> CalculationStatus { get; set; } = default!;
        public DbSet<CalculationEntity> Calculations { get; set; } = default!;
        public DbSet<TaskEntity> Tasks { get; set; } = default!;
        public DbSet<ResourceEntity> Resources { get; set; } = default!;
        public DbSet<ResourceTypeEntity> ResourceTypes { get; set; } = default!;
        public DbSet<OrganisationTypeEntity> OrganisationType { get; set; } = default!;
        public DbSet<StorageEntity> Storages { get; set; } = default!;
        public DbSet<TemplateEntity> Templates { get; set; } = default!;
        public DbSet<ResourceSortEntity> ResourceSorts { get; set; } = default!;

        public DbSet<ApplicationEntity> Applications { get; set; } = default!;
        public DbSet<ApplicationValuesEntity> ApplicationValues { get; set; } = default!;
        public DbSet<TypeEntity> CalcProjectType { get; set; } = default!;
        public DbSet<TaskStatusEntity> TaskStatus { get; set; } = default!;
        public DbSet<ProcurementMethodEntity> ProcurementMethod { get; set; } = default!;
        public DbSet<CompensationEntity> Compensations { get; set; } = default!;
        public DbSet<ContractEntity> Contracts { get; set; } = default!;
        public DbSet<OfferEntity> Offers { get; set; } = default!;
        public DbSet<OrganisationCategoryEntity> OrganisationCategory { get; set; } = default!;
        public DbSet<OrganisationEntity> Organisation { get; set; } = default!;
        public DbSet<AccountGroupEntity> AccountGroup { get; set; } = default!;
        public DbSet<AccountEntity> Accounts { get; set; } = default!;
        public DbSet<ShareCalcEntity> ShareCalc { get; set; } = default!;
        public DbSet<OpportunityEntity> Opportunity { get; set; } = default!;
        public DbSet<DepartmentEntity> Department { get; set; } = default!;
        public DbSet<StatusResourcesEntity> ResourceStatus { get; set; } = default!;
        public DbSet<UserEntity> User { get; set; } = default!;
        public DbSet<TenderAttributeDefinitionEntity> AttributeNameTender { get; set; } = default!;
        public DbSet<TenderAttributeBindEntity> TenderAttributeBind { get; set; } = default!;

        #endregion

        #region SaveChanges / Tenant handling

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTenantId();
            UpdateAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }
        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries<IAuditable>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            var now = DateTime.UtcNow;
            var userId = CurrentUserId ?? 0; // أو null إذا جعلتها int?

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                }

                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;
            }
        }
        private void UpdateTenantId()
        {
            var entries = ChangeTracker
                .Entries<IDataKeyFilterReadOnly>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                entry.Entity.TenantId = TenantId;
            }
        }

        #endregion
    }
}
