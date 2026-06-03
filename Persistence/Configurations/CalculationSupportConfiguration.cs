using Domain.Entities.Calculation;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class OpportunityConfiguration : IEntityTypeConfiguration<OpportunityEntity>
{
    public void Configure(EntityTypeBuilder<OpportunityEntity> builder)
    {
        builder.ToTable("Opportunities");

        builder.HasIndex(x => new { x.TenantId, x.CalculationId })
            .HasDatabaseName("IX_Opportunities_Tenant_Calc");

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Opportunities_Risks_NotEmpty", "LEN(LTRIM(RTRIM([OpportunitiesRisks]))) > 0")
        );

        builder.HasOne(x => x.Calculation)
            .WithMany(x => x.Opportunities)
            .HasForeignKey(x => x.CalculationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ShareCalcConfiguration : IEntityTypeConfiguration<ShareCalcEntity>
{
    public void Configure(EntityTypeBuilder<ShareCalcEntity> builder)
    {
        builder.ToTable("ShareCalcs");

        builder.HasIndex(x => new { x.TenantId, x.CalculationId, x.DepartmentId })
            .IsUnique()
            .HasDatabaseName("UX_ShareCalcs_Tenant_Calc_Department");

        builder.HasIndex(x => new { x.TenantId, x.CalculationId })
            .HasDatabaseName("IX_ShareCalcs_Tenant_Calc");

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId })
            .HasDatabaseName("IX_ShareCalcs_Tenant_Department");

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Calculation)
            .WithMany(x => x.SharesCalc)
            .HasForeignKey(x => x.CalculationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class StorageConfiguration : IEntityTypeConfiguration<StorageEntity>
{
    public void Configure(EntityTypeBuilder<StorageEntity> builder)
    {
        builder.ToTable("Storages");

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.StorageType, x.StorageSort, x.StorageLevel })
            .HasDatabaseName("IX_Storages_Tenant_Department_Type_Sort_Level");

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Storages_Value_NotEmpty", "LEN(LTRIM(RTRIM([StorageValue]))) > 0")
        );

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class StatusResourcesConfiguration : IEntityTypeConfiguration<StatusResourcesEntity>
{
    public void Configure(EntityTypeBuilder<StatusResourcesEntity> builder)
    {
        builder.ToTable("StatusResources");

        builder.HasIndex(x => new { x.TenantId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_StatusResources_Tenant_Visible_Order");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_StatusResources_Tenant_Name");

        builder.Property(x => x.Code).HasMaxLength(64);

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .HasFilter("[Code] <> ''")
            .HasDatabaseName("IX_StatusResources_Tenant_Code");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_StatusResources_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_StatusResources_Color_Hex", LookupChecks.HexColorCheck);
            t.HasCheckConstraint("CK_StatusResources_SortOrder_NonNegative", LookupChecks.NonNegativeSortOrderCheck);
        });
    }
}

internal sealed class TaskStatusConfiguration : IEntityTypeConfiguration<TaskStatusEntity>
{
    public void Configure(EntityTypeBuilder<TaskStatusEntity> builder)
    {
        builder.ToTable("TaskStatuses");

        builder.HasIndex(x => new { x.TenantId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_TaskStatuses_Tenant_Visible_Order");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_TaskStatuses_Tenant_Name");

        builder.Property(x => x.Code).HasMaxLength(64);

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .HasFilter("[Code] <> ''")
            .HasDatabaseName("IX_TaskStatuses_Tenant_Code");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_TaskStatuses_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_TaskStatuses_Color_Hex", LookupChecks.HexColorCheck);
            t.HasCheckConstraint("CK_TaskStatuses_SortOrder_NonNegative", LookupChecks.NonNegativeSortOrderCheck);
        });
    }
}
