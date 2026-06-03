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

        // Apply all IEntityTypeConfiguration<> in this assembly.
        // TenderAttributeBind/Tender relations are now owned بالكامل by TenderConfiguration
        // لتجنب تعريف العلاقة نفسها أكثر من مرة بمفاتيح/OnDelete مختلفة.
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
            .HasJsonConversion<TemplateMetadataData>();
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

        modelBuilder.Entity<ProjectStatusEntity>()
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

            // + Parent SoftDelete filter (للكيانات التي لا تدعم SoftDelete لكنها تابعة لكيان SoftDelete)
            // مثال: Tasks/Resources/Offers/Tenders... يجب ألا تظهر إذا كان Calculation أو Project محذوف (Soft).
            var cascadeSoftDelete = BuildCascadeSoftDeleteFilter(parameter, clrType);
            if (cascadeSoftDelete != null)
                body = Expression.AndAlso(body, cascadeSoftDelete);

            var lambda = Expression.Lambda(body, parameter);
            modelBuilder.Entity(clrType).HasQueryFilter(lambda);
        }
    }

    /// <summary>
    /// يضيف شرط إضافي لبعض الجداول التي تعتمد على Calculation/Project (التي تدعم SoftDelete)
    /// حتى لا تظهر بيانات يتيمة عند حذف Project/Calculation كـ SoftDelete.
    /// </summary>
    private static Expression? BuildCascadeSoftDeleteFilter(ParameterExpression parameter, Type clrType)
    {
        // Calculation itself is soft-deletable already, but we also want to hide it when Project is soft-deleted.
        if (clrType == typeof(CalculationEntity))
        {
            var project = Expression.Property(parameter, nameof(CalculationEntity.Project));
            return NotDeleted(project);
        }

        // Entities that directly reference Calculation
        if (clrType == typeof(TaskEntity)
            || clrType == typeof(ShareCalcEntity)
            || clrType == typeof(OpportunityEntity)
            || clrType == typeof(ApplicationValuesEntity)
            || clrType == typeof(TenderEntity)
            || clrType == typeof(TenderAttributeDefinitionEntity))
        {
            var calc = Expression.Property(parameter, "Calculation");
            var project = Expression.Property(calc, nameof(CalculationEntity.Project));
            return Expression.AndAlso(NotDeleted(calc), NotDeleted(project));
        }

        // TenderAttributeBind -> Tender -> Calculation -> Project
        if (clrType == typeof(TenderAttributeBindEntity))
        {
            var tender = Expression.Property(parameter, nameof(TenderAttributeBindEntity.Tender));
            var calc = Expression.Property(tender, nameof(TenderEntity.Calculation));
            var project = Expression.Property(calc, nameof(CalculationEntity.Project));
            return Expression.AndAlso(NotDeleted(calc), NotDeleted(project));
        }

        // Resource -> Task -> Calculation -> Project
        if (clrType == typeof(ResourceEntity))
        {
            var task = Expression.Property(parameter, nameof(ResourceEntity.Task));
            var calc = Expression.Property(task, nameof(TaskEntity.Calculation));
            var project = Expression.Property(calc, nameof(CalculationEntity.Project));
            return Expression.AndAlso(NotDeleted(calc), NotDeleted(project));
        }

        // Offer -> Resource -> Task -> Calculation -> Project
        if (clrType == typeof(OfferEntity))
        {
            var res = Expression.Property(parameter, nameof(OfferEntity.Resource));
            var task = Expression.Property(res, nameof(ResourceEntity.Task));
            var calc = Expression.Property(task, nameof(TaskEntity.Calculation));
            var project = Expression.Property(calc, nameof(CalculationEntity.Project));
            return Expression.AndAlso(NotDeleted(calc), NotDeleted(project));
        }

        return null;
    }

    private static Expression NotDeleted(Expression softDeletableEntity)
    {
        // entity.IsDeleted == false
        var isDeleted = Expression.Property(softDeletableEntity, nameof(ISoftDeletable.IsDeleted));
        return Expression.Equal(isDeleted, Expression.Constant(false));
    }

    #endregion
}
