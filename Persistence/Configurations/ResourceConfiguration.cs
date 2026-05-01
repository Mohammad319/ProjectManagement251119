using Domain.Entities.Calculation;
using Domain.Entities.ResourceType;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Serialization;
using ProjectManagement.Shared.Constant;

namespace Persistence.Configurations;

internal static class CalculationMetadataComputedColumns
{
    internal static string JsonString(string propertyName, int maxLength)
        => $"CAST(NULLIF(LTRIM(RTRIM(JSON_VALUE([Metadata], '$.{propertyName}'))), '') AS nvarchar({maxLength}))";

    internal static string JsonBool(string propertyName, bool defaultValue)
        => $"CAST(CASE LOWER(JSON_VALUE([Metadata], '$.{propertyName}')) WHEN 'true' THEN 1 WHEN '1' THEN 1 WHEN 'false' THEN 0 WHEN '0' THEN 0 ELSE {(defaultValue ? 1 : 0)} END AS bit)";

    internal static string JsonInt(string propertyName, int defaultValue)
        => $"COALESCE(TRY_CONVERT(int, JSON_VALUE([Metadata], '$.{propertyName}')), {defaultValue})";

    internal static string JsonDecimal(string propertyName, string precision = "18,3")
        => $"TRY_CONVERT(decimal({precision}), JSON_VALUE([Metadata], '$.{propertyName}'))";
}

internal sealed class ResourceTypeConfiguration : IEntityTypeConfiguration<ResourceTypeEntity>
{
    public void Configure(EntityTypeBuilder<ResourceTypeEntity> builder)
    {
        builder.ToTable("ResourceTypes");

        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        builder.HasOne(x => x.Account)
            .WithMany(x => x.ResourceTypes)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasIndex(x => new { x.TenantId, x.Kind, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_ResourceTypes_Tenant_Kind_Visible_Order");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .HasDatabaseName("IX_ResourceTypes_Tenant_Name");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ResourceTypes_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_ResourceTypes_SortOrder_NonNegative", "[SortOrder] >= 0");
            t.HasCheckConstraint("CK_ResourceTypes_Account_Positive", "[AccountId] IS NULL OR [AccountId] > 0");
        });
    }
}

internal sealed class ResourceSortConfiguration : IEntityTypeConfiguration<ResourceSortEntity>
{
    public void Configure(EntityTypeBuilder<ResourceSortEntity> builder)
    {
        builder.ToTable("ResourceSorts");

        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        builder.HasOne(x => x.ResourceType)
            .WithMany(x => x.ResourcesSort)
            .HasForeignKey(x => x.ResourceTypeId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Account)
            .WithMany(x => x.ResourceSorts)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasIndex(x => new { x.TenantId, x.ResourceTypeId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_ResourceSorts_Tenant_Type_Visible_Order");

        builder.HasIndex(x => new { x.TenantId, x.ResourceTypeId, x.Name })
            .HasDatabaseName("IX_ResourceSorts_Tenant_Type_Name");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ResourceSorts_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_ResourceSorts_SortOrder_NonNegative", "[SortOrder] >= 0");
            t.HasCheckConstraint("CK_ResourceSorts_Account_Positive", "[AccountId] IS NULL OR [AccountId] > 0");
        });
    }
}

internal sealed class ResourceConfiguration : IEntityTypeConfiguration<ResourceEntity>
{
    public void Configure(EntityTypeBuilder<ResourceEntity> builder)
    {
        builder.ToTable("Resources", t =>
        {
            t.HasCheckConstraint("CK_Resources_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_Resources_SortOrder_NonNegative", "[SortOrder] >= 0");
            t.HasCheckConstraint("CK_Resources_Task_Positive", "[TaskId] > 0");
        });

        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        builder.Property(x => x.Note)
            .HasComputedColumnSql(
                CalculationMetadataComputedColumns.JsonString(nameof(ResourceEntity.Note), FieldLengths.Comment),
                stored: true);

        builder.Property(x => x.Unit)
            .HasComputedColumnSql(
                CalculationMetadataComputedColumns.JsonString(nameof(ResourceEntity.Unit), FieldLengths.Unit),
                stored: true);

        builder.Property(x => x.Quantity)
            .HasColumnType("decimal(18,3)")
            .HasComputedColumnSql(
                CalculationMetadataComputedColumns.JsonDecimal(nameof(ResourceEntity.Quantity)),
                stored: true);

        builder.HasIndex(x => new { x.TenantId, x.TaskId, x.SortOrder })
            .HasDatabaseName("IX_Resources_Tenant_Task_Sort");

        builder.HasIndex(x => new { x.TenantId, x.ResourceTypeId })
            .HasDatabaseName("IX_Resources_Tenant_ResourceType");

        builder.HasIndex(x => new { x.TenantId, x.ResourceSortId })
            .HasDatabaseName("IX_Resources_Tenant_ResourceSort");

        builder.HasIndex(x => new { x.TenantId, x.AccountId })
            .HasDatabaseName("IX_Resources_Tenant_Account");

        builder.HasIndex(x => new { x.TenantId, x.StatusId })
            .HasDatabaseName("IX_Resources_Tenant_Status");

        builder.HasIndex(x => new { x.TenantId, x.OpportunityId })
            .HasDatabaseName("IX_Resources_Tenant_Opportunity");

        builder.HasOne(x => x.ResourceSort)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.ResourceSortId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.ResourceType)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.ResourceTypeId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Account)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Status)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Opportunity)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.OpportunityId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}

internal sealed class TaskConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.ToTable("Tasks", t =>
        {
            t.HasCheckConstraint("CK_Tasks_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_Tasks_SortOrder_NonNegative", "[SortOrder] >= 0");
            t.HasCheckConstraint("CK_Tasks_Calculation_Positive", "[CalculationId] > 0");
        });

        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        builder.Property(x => x.NormalizedTextSv)
            .HasMaxLength(FieldLengths.NormalizedText);

        builder.Property(x => x.Note)
            .HasComputedColumnSql(
                CalculationMetadataComputedColumns.JsonString(nameof(TaskEntity.Note), FieldLengths.Comment),
                stored: true);

        builder.Property(x => x.Unit)
            .HasComputedColumnSql(
                CalculationMetadataComputedColumns.JsonString(nameof(TaskEntity.Unit), FieldLengths.Unit),
                stored: true);

        builder.Property(x => x.Code)
            .HasComputedColumnSql(
                CalculationMetadataComputedColumns.JsonString(nameof(TaskEntity.Code), FieldLengths.Code),
                stored: true);

        builder.Property(x => x.IsActive)
            .HasComputedColumnSql(
                CalculationMetadataComputedColumns.JsonBool(nameof(TaskEntity.IsActive), defaultValue: true),
                stored: true);

        builder.Property(x => x.IsOH)
            .HasComputedColumnSql(
                CalculationMetadataComputedColumns.JsonBool(nameof(TaskEntity.IsOH), defaultValue: false),
                stored: true);

        builder.Property(x => x.Type)
            .HasComputedColumnSql(
                CalculationMetadataComputedColumns.JsonInt(nameof(TaskEntity.Type), defaultValue: 0),
                stored: true);

        builder.Property(x => x.Quantity)
            .HasColumnType("decimal(18,3)")
            .HasComputedColumnSql(
                CalculationMetadataComputedColumns.JsonDecimal(nameof(TaskEntity.Quantity)),
                stored: true);

        builder.HasOne(x => x.Status)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Opportunity)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.OpportunityId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.ParentTask)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.ParentTaskId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne(x => x.Calculation)
            .WithMany(c => c.Tasks)
            .HasForeignKey(x => x.CalculationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.CalculationId, x.ParentTaskId, x.SortOrder })
            .HasDatabaseName("IX_Tasks_Tenant_Calc_Parent_Sort");

        builder.HasIndex(x => new { x.TenantId, x.CalculationId, x.StatusId })
            .HasDatabaseName("IX_Tasks_Tenant_Calc_Status");

        builder.HasIndex(x => new { x.TenantId, x.ParentTaskId })
            .HasDatabaseName("IX_Tasks_Tenant_Parent");

        builder.HasIndex(x => new { x.TenantId, x.NormalizedTextSv })
            .HasDatabaseName("IX_Tasks_Tenant_NormalizedTextSv");
    }
}

internal sealed class OfferConfiguration : IEntityTypeConfiguration<OfferEntity>
{
    public void Configure(EntityTypeBuilder<OfferEntity> builder)
    {
        builder.ToTable("Offers");

        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasOne(o => o.Resource)
            .WithMany(r => r.Offers)
            .HasForeignKey(o => o.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Organisation)
            .WithMany(p => p.Offers)
            .HasForeignKey(o => o.OrganisationId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        builder.Property<decimal?>("CostValue")
            .HasColumnType("decimal(18,2)")
            .HasComputedColumnSql("TRY_CONVERT(decimal(18,2), JSON_VALUE([Metadata], '$.Cost'))", stored: true);

        builder.Property<decimal?>("BaseCostValue")
            .HasColumnType("decimal(18,2)")
            .HasComputedColumnSql("TRY_CONVERT(decimal(18,2), JSON_VALUE([Metadata], '$.BaseCost'))", stored: true);

        builder.HasIndex("TenantId", "CostValue")
            .HasDatabaseName("IX_Offers_Tenant_CostValue");

        builder.HasIndex("TenantId", "BaseCostValue")
            .HasDatabaseName("IX_Offers_Tenant_BaseCostValue");

        builder.HasIndex(x => new { x.TenantId, x.OrganisationId, x.Date })
            .HasDatabaseName("IX_Offers_Tenant_Org_Date");
    }
}
