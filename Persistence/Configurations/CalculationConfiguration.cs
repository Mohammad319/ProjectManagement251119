using Domain.Entities.Users;
using Persistence.Serialization;

namespace Persistence.Configurations;

using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class CalculationConfiguration : IEntityTypeConfiguration<CalculationEntity>
{
    public void Configure(EntityTypeBuilder<CalculationEntity> builder)
    {
        builder.ToTable("Calculations");

        builder.Property(e => e.Metadata).HasJsonConversion();
        builder.Property(e => e.Sort).HasJsonConversion();
        builder.Property(e => e.HourlyPrice).HasJsonConversion();
        builder.Property(e => e.Factors).HasJsonConversion();
        builder.Property(e => e.DisplayPresets).HasJsonConversion();
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.CalculationType).HasConversion<int>();
        builder.Property(e => e.BidRole).HasConversion<int>();
        builder.Property(e => e.CalculationRole).HasConversion<int>();
        builder.Property(e => e.CustomCalculationRoleName).HasMaxLength(80);

        builder.HasOne(x => x.ProcurementMethods)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.ProcurementMethodsId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Type)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.TypeId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Compensation)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.CompensationId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Contract)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Status)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Template)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.TemplateColumn)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.TemplateColumnId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Organisation)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.OrganisationId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // DepartmentId مُخزَّن كـ denormalized field لأغراض الأداء
        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Calculations_Tax_0_100", "[Tax] >= 0 AND [Tax] <= 100");
            t.HasCheckConstraint("CK_Calculations_DateRange", "[EndDate] >= [StartDate]");
            t.HasCheckConstraint("CK_Calculations_Code_NotEmpty", "LEN(LTRIM(RTRIM([Code]))) > 0");
            t.HasCheckConstraint("CK_Calculations_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_Calculations_CalculationType", "[CalculationType] IN (0, 1, 2)");
            t.HasCheckConstraint("CK_Calculations_BidRole", "[BidRole] IN (0, 1, 2, 3)");
            t.HasCheckConstraint("CK_Calculations_CalculationRole", "[CalculationRole] IN (0, 1, 2, 3, 4, 5)");
        });

        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.DepartmentId, x.SortOrder })
            .HasDatabaseName("IX_Calculations_Tenant_Project_Department_Order");

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId })
            .HasDatabaseName("IX_Calculations_Tenant_Department");

        builder.HasIndex(x => new { x.TenantId, x.Id, x.DepartmentId })
            .HasDatabaseName("IX_Calculations_Tenant_Id_Department");

        builder.HasIndex(x => new { x.TenantId, x.ProjectId })
            .HasDatabaseName("IX_Calculations_Tenant_Project");

        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Calculations_Tenant_Project_Code");

        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.IsPrivate, x.SortOrder })
            .HasDatabaseName("IX_Calculations_Tenant_Project_Private_Order");

        builder.HasIndex(x => new { x.TenantId, x.StatusId })
            .HasDatabaseName("IX_Calculations_Tenant_Status");

        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.VersionGroupId, x.VersionNumber })
            .HasDatabaseName("IX_Calculations_Tenant_Project_VersionGroup_Number");

        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.VersionGroupId, x.IsCurrentVersion })
            .HasDatabaseName("IX_Calculations_Tenant_Project_VersionGroup_Current");

        builder.HasIndex("TenantId", "CreatedBy")
            .HasDatabaseName("IX_Calculations_Tenant_CreatedBy");
    }
}
