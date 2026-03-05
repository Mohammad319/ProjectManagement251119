using Domain.Entities.Application;
using Domain.Entities.Calculation;
using Domain.Entities.Organisation;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

// ============================================================================
// Entities التي كانت تعتمد على conventions فقط.
// هدف هذا الملف: (indexes + constraints + multi-tenant safety + naming)
// بدون فرض تغييرات كبيرة على الـ domain models.
// ============================================================================

internal sealed class AccountGroupConfiguration : IEntityTypeConfiguration<AccountGroupEntity>
{
    public void Configure(EntityTypeBuilder<AccountGroupEntity> builder)
    {
        builder.ToTable("AccountGroups");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("UX_AccountGroups_Tenant_Name");

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_AccountGroups_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0")
        );
    }
}

internal sealed class AccountConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.ToTable("Accounts");

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasDatabaseName("UX_Accounts_Tenant_Code");

        builder.HasIndex(x => new { x.TenantId, x.AccountGroupId })
            .HasDatabaseName("IX_Accounts_Tenant_Group");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .HasDatabaseName("IX_Accounts_Tenant_Name");

        builder.HasOne(x => x.AccountGroup)
            .WithMany()
            .HasForeignKey(x => x.AccountGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Accounts_Code_NotEmpty", "LEN(LTRIM(RTRIM([Code]))) > 0");
            t.HasCheckConstraint("CK_Accounts_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
        });
    }
}

internal sealed class OrganisationConfiguration : IEntityTypeConfiguration<OrganisationEntity>
{
    public void Configure(EntityTypeBuilder<OrganisationEntity> builder)
    {
        builder.ToTable("Organisations");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .HasDatabaseName("IX_Organisations_Tenant_Name");

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Organisations_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0")
        );
    }
}

internal sealed class ShareCalcConfiguration : IEntityTypeConfiguration<ShareCalcEntity>
{
    public void Configure(EntityTypeBuilder<ShareCalcEntity> builder)
    {
        builder.ToTable("ShareCalcs");

        builder.HasOne(x => x.Calculation)
            .WithMany(x => x.SharesCalc)
            .HasForeignKey(x => x.CalculationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // منع التكرار: نفس القسم لا يجب أن يشارك نفس calculation أكثر من مرة داخل نفس tenant
        builder.HasIndex(x => new { x.TenantId, x.CalculationId, x.DepartmentId })
            .IsUnique()
            .HasDatabaseName("UX_ShareCalcs_Tenant_Calc_Department");

        builder.HasIndex(x => new { x.TenantId, x.CalculationId })
            .HasDatabaseName("IX_ShareCalcs_Tenant_Calc");
    }
}

internal sealed class OpportunityConfiguration : IEntityTypeConfiguration<OpportunityEntity>
{
    public void Configure(EntityTypeBuilder<OpportunityEntity> builder)
    {
        builder.ToTable("Opportunities");

        builder.HasOne(x => x.Calculation)
            .WithMany(x => x.Opportunities)
            .HasForeignKey(x => x.CalculationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.CalculationId })
            .HasDatabaseName("IX_Opportunities_Tenant_Calc");

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Opportunities_Text_NotEmpty", "LEN(LTRIM(RTRIM([OpportunitiesRisks]))) > 0")
        );
    }
}

internal sealed class StorageConfiguration : IEntityTypeConfiguration<StorageEntity>
{
    public void Configure(EntityTypeBuilder<StorageEntity> builder)
    {
        builder.ToTable("Storages");

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.StorageType, x.StorageSort, x.StorageLevel })
            .HasDatabaseName("IX_Storages_Tenant_Department_Type_Sort_Level");
    }
}

internal sealed class ApplicationConfiguration : IEntityTypeConfiguration<ApplicationEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationEntity> builder)
    {
        builder.ToTable("Applications");

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.IsVisible, x.Id })
            .HasDatabaseName("IX_Applications_Tenant_Department_Visible_Id");

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId })
            .HasDatabaseName("IX_Applications_Tenant_Department");

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Applications_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0")
        );
    }
}

internal sealed class ApplicationValuesConfiguration : IEntityTypeConfiguration<ApplicationValuesEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationValuesEntity> builder)
    {
        builder.ToTable("ApplicationValues");

        builder.HasOne(x => x.Calculation)
            .WithMany(x => x.Applications)
            .HasForeignKey(x => x.CalculationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Application)
            .WithMany()
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.CalculationId })
            .HasDatabaseName("IX_ApplicationValues_Tenant_Calc");

        builder.HasIndex(x => new { x.TenantId, x.ApplicationId })
            .HasDatabaseName("IX_ApplicationValues_Tenant_App");

        // منع إدخال نفس Application مرتين لنفس Calculation داخل نفس Tenant
        builder.HasIndex(x => new { x.TenantId, x.CalculationId, x.ApplicationId })
            .IsUnique()
            .HasDatabaseName("UX_ApplicationValues_Tenant_Calc_App");

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_ApplicationValues_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0")
        );
    }
}
