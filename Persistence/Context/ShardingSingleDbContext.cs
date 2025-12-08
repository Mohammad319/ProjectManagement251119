using Application.Interfaces.Context;
using Domain.Entities.Application;
using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using Domain.Entities.Project;
using Domain.Entities.ResourceType;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Persistence.Configurations;
using ProjectManagement.Shared.Base.Application;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using ProjectManagement.Shared.DTO.Organisation;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Persistence.Context
{
    public class ShardingSingleDbContext : DbContext, IShardingSingleDbContext
    {
        public int TenantId { get; set; }

        public ShardingSingleDbContext(DbContextOptions<ShardingSingleDbContext> options)
            : base(options)
        {
        }
        /// <summary>
        /// قيمة التينانت الحالية، يجب تعيينها قبل تنفيذ أي استعلام.
        /// </summary>
        public int TenantId { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // في الغالب ستستخدم DI لتمرير الـ ConnectionString
            // وإذا احتجت لقراءة من خدمة TenantService يمكن إضافتها هنا
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureTenderAttributeRelations(modelBuilder);
            ConfigureEntityConfigurations(modelBuilder);
            ConfigureJsonDataConversions(modelBuilder);
            ConfigureOrderSequences(modelBuilder);
            ConfigureGlobalTenantFilter(modelBuilder);
        }

        #region Configuration helpers

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
            // يمكنك الاستغناء عن هذه المجموعة واستخدام:
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
                .Property(e => e.Data)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<AccountData>(v, JsonSerializerOptions.Default) ?? new AccountData());

            modelBuilder.Entity<OrganisationEntity>()
                .Property(e => e.Data)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<OrganisationData>(v, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new OrganisationData());

            modelBuilder.Entity<OpportunityEntity>()
                .Property(e => e.Data)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<OpportunityData>(v, JsonSerializerOptions.Default) ?? new OpportunityData());

            modelBuilder.Entity<ShareCalcEntity>()
                .Property(e => e.Data)
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
                .Property(e => e.Data)
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
                .Property(o => o.Order)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<TaskStatusEntity>()
                .Property(o => o.Order)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<StatusResourcesEntity>()
                .Property(o => o.Order)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<TypeEntity>()
                .Property(o => o.Order)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<ContractEntity>()
                .Property(o => o.Order)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<CompensationEntity>()
                .Property(o => o.Order)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<ProcurementMethodsEntity>()
                .Property(o => o.Order)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");

            modelBuilder.Entity<ResourceTypeEntity>()
                .Property(o => o.Order)
                .HasDefaultValueSql("NEXT VALUE FOR OrderSeq");
        }

        /// <summary>
        /// تطبيق Global Query Filter لكل الكيانات التي تطبّق IDataKeyFilterReadOnly
        /// بحيث يتم فلترتها تلقائياً حسب TenantId الحالي.
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

        public DbSet<TenderEntity> Tender { get; set; }
        public DbSet<ProjectEntity> Project { get; set; }
        public DbSet<FolderEntity> Folder { get; set; }
        public DbSet<StatusEntity> CalculationStatus { get; set; }
        public DbSet<CalculationEntity> Calculation { get; set; }
        public DbSet<TaskEntity> Tasks { get; set; }
        public DbSet<ResourceEntity> Resource { get; set; }
        public DbSet<ResourceTypeEntity> ResourceType { get; set; }
        public DbSet<OrganisationTypeEntity> OrganisationType { get; set; }
        public DbSet<StorageEntity> Storage { get; set; }
        public DbSet<TemplateEntity> Template { get; set; }
        public DbSet<ResourceSortEntity> ResourceSort { get; set; }

        public DbSet<ApplicationEntity> Application { get; set; }
        public DbSet<ApplicationValuesEntity> ApplicationValues { get; set; }
        public DbSet<TypeEntity> CalcProjectType { get; set; }
        public DbSet<TaskStatusEntity> TaskStatus { get; set; }
        public DbSet<ProcurementMethodsEntity> ProcurementMethod { get; set; }
        public DbSet<CompensationEntity> Compensation { get; set; }
        public DbSet<ContractEntity> Contract { get; set; }
        public DbSet<OfferEntity> Offer { get; set; }
        public DbSet<OrganisationCategoryEntity> OrganisationCategory { get; set; }
        public DbSet<OrganisationEntity> Organisation { get; set; }
        public DbSet<AccountGroupEntity> AccountGroup { get; set; }
        public DbSet<AccountEntity> Account { get; set; }
        public DbSet<ShareCalcEntity> ShareCalc { get; set; }
        public DbSet<OpportunityEntity> Opportunity { get; set; }
        public DbSet<DepartmentEntity> Department { get; set; }
        public DbSet<StatusResourcesEntity> ResourceStatus { get; set; }
        public DbSet<UserEntity> User { get; set; }
        public DbSet<AttributeNameTenderEntity> AttributeNameTender { get; set; }
        public DbSet<TenderAttributeBindEntity> TenderAttributeBind { get; set; }

        #endregion

        #region SaveChanges / Tenant Handling

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTenantId();
            return await base.SaveChangesAsync(cancellationToken);
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
