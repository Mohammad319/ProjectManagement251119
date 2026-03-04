using Domain.Entities.Application;
using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Organisation;
using Domain.Entities.Project;
using Domain.Entities.ResourceType;
using Domain.Entities.Users;
using Persistence.Serialization;
using ProjectManagement.Shared.Base.Application;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using ProjectManagement.Shared.DTO.Organisation;
using System.Linq.Expressions;

namespace Persistence.Context;

public partial class ShardingSingleDbContext(DbContextOptions<ShardingSingleDbContext> options) : DbContext(options)
{
    /// <summary>
    /// قيمة الـ Tenant الحالية، يجب تعيينها من الطبقة الأعلى (Middleware / Service).
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// المستخدم الحالي (للـ Audit). يفضّل تعبئته من الـ middleware/service.
    /// </summary>
    public int? CurrentUserId { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureAuditUserRelations(modelBuilder);
        ConfigureTenderAttributeRelations(modelBuilder);

        // Apply all IEntityTypeConfiguration<> in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShardingSingleDbContext).Assembly);

        ConfigureJsonDataConversions(modelBuilder);
        ConfigureOrderSequences(modelBuilder);

        ConfigureGlobalTenantFilter(modelBuilder);
    }

    #region Configuration

    private static void ConfigureAuditUserRelations(ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t =>
                typeof(AuditableEntity<>).IsAssignableFromGeneric(t.ClrType) ||
                typeof(AuditableSoftDeletableEntity<>).IsAssignableFromGeneric(t.ClrType));

        foreach (var entityType in entityTypes)
        {
            var entityBuilder = modelBuilder.Entity(entityType.ClrType);

            // CreatedBy
            entityBuilder
                .HasOne(typeof(UserEntity), nameof(AuditableEntity<int>.CreatedByUser))
                .WithMany()
                .HasForeignKey(nameof(AuditableEntity<int>.CreatedBy))
                .OnDelete(DeleteBehavior.Restrict);

            // UpdatedBy
            entityBuilder
                .HasOne(typeof(UserEntity), nameof(AuditableEntity<int>.UpdatedByUser))
                .WithMany()
                .HasForeignKey(nameof(AuditableEntity<int>.UpdatedBy))
                .OnDelete(DeleteBehavior.Restrict);

            // DeletedBy (فقط للـ SoftDelete)
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                entityBuilder
                    .HasOne(typeof(UserEntity), nameof(AuditableSoftDeletableEntity<int>.DeletedByUser))
                    .WithMany()
                    .HasForeignKey(nameof(AuditableSoftDeletableEntity<int>.DeletedBy))
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }
    }

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

    /// <summary>
    /// JSON conversions الخاصة ببعض Entities (إن كانت موجودة كـ properties string JSON).
    /// استعملنا HasJsonConversion لتقليل التكرار + توحيد JsonOptions.
    /// </summary>
    private static void ConfigureJsonDataConversions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>()
            .Property(e => e.Metadata)
            .HasJsonConversion<AccountData>();

        modelBuilder.Entity<OrganisationEntity>()
            .Property(e => e.Metadata)
            .HasJsonConversion<OrganisationData>();

        modelBuilder.Entity<OpportunityEntity>()
            .Property(e => e.Metadata)
            .HasJsonConversion<OpportunityData>();

        modelBuilder.Entity<ShareCalcEntity>()
            .Property(e => e.Metadata)
            .HasJsonConversion<ShareCalcData>();

        modelBuilder.Entity<ApplicationEntity>()
            .Property(e => e.Data)
            .HasJsonConversion<ApplicationDataEntity>();

        modelBuilder.Entity<ApplicationValuesEntity>()
            .Property(e => e.Data)
            .HasJsonConversion<ApplicationValuesData>();

        modelBuilder.Entity<TemplateEntity>()
            .Property(e => e.Metadata)
            .HasJsonConversion<TemplateData>();
    }

    private static void ConfigureOrderSequences(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>("OrderSeq")
            .StartsAt(0)
            .IncrementsBy(100);

        // استخدم nameof لتقليل أخطاء refactoring
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
    /// 
    private void ConfigureGlobalTenantFilter(ModelBuilder modelBuilder)
    {
        var tenantEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(IDataKeyFilterReadOnly).IsAssignableFrom(t.ClrType))
            .Select(t => t.ClrType)
            .Distinct()
            .ToList();

        foreach (var clrType in tenantEntityTypes)
        {
            var parameter = Expression.Parameter(clrType, "e");

            // EF.Property<int>(e, "TenantId")
            var tenantIdProperty = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                new[] { typeof(int) },
                parameter,
                Expression.Constant(nameof(IDataKeyFilterReadOnly.TenantId)));

            // this.TenantId
            var tenantIdValue = Expression.Property(Expression.Constant(this), nameof(TenantId));

            // EF.Property<int>(e, "TenantId") == this.TenantId
            Expression body = Expression.Equal(tenantIdProperty, tenantIdValue);

            // + SoftDelete filter (إذا الكيان يطبق ISoftDeletable)
            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                // EF.Property<bool>(e, "IsDeleted")
                var isDeletedProperty = Expression.Call(
                    typeof(EF),
                    nameof(EF.Property),
                    new[] { typeof(bool) },
                    parameter,
                    Expression.Constant(nameof(ISoftDeletable.IsDeleted)));

                // EF.Property<bool>(e, "IsDeleted") == false
                var notDeleted = Expression.Equal(isDeletedProperty, Expression.Constant(false));

                body = Expression.AndAlso(body, notDeleted);
            }

            var lambda = Expression.Lambda(body, parameter);
            modelBuilder.Entity(clrType).HasQueryFilter(lambda);
        }
    }

    #endregion
}
