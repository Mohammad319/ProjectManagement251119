using Domain.Entities.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class ApplicationConfiguration : IEntityTypeConfiguration<ApplicationEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationEntity> builder)
    {
        builder.ToTable("Applications");

        // Department listing (tenant + department)
        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.IsVisible, x.Id })
            .HasDatabaseName("IX_Applications_Tenant_Department_Visible_Id");

        // CreatedBy / UserId filters
        builder.HasIndex(x => new { x.TenantId, x.UserId })
            .HasDatabaseName("IX_Applications_Tenant_User");

        // Quality
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Applications_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0")
        );

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ApplicationValuesConfiguration : IEntityTypeConfiguration<ApplicationValuesEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationValuesEntity> builder)
    {
        builder.ToTable("ApplicationValues");

        // Multi-tenant safety: نفس Application لا يتكرر لنفس Calculation داخل نفس Tenant
        builder.HasIndex(x => new { x.TenantId, x.CalculationId, x.ApplicationId })
            .IsUnique()
            .HasDatabaseName("UX_ApplicationValues_Tenant_Calc_App");

        builder.HasIndex(x => new { x.TenantId, x.CalculationId, x.LastUpdate })
            .HasDatabaseName("IX_ApplicationValues_Tenant_Calc_LastUpdate");

        builder.HasIndex(x => new { x.TenantId, x.ApplicationId })
            .HasDatabaseName("IX_ApplicationValues_Tenant_App");

        builder.HasIndex(x => new { x.TenantId, x.UserId })
            .HasDatabaseName("IX_ApplicationValues_Tenant_User");

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_ApplicationValues_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0")
        );

        builder.HasOne(x => x.Application)
            .WithMany()
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Calculation)
            .WithMany(x => x.Applications)
            .HasForeignKey(x => x.CalculationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
