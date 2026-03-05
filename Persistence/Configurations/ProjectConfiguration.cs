using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Serialization;
using ProjectManagement.Shared.DTO.Project;

namespace Persistence.Configurations;


public sealed class ProjectConfiguration : IEntityTypeConfiguration<ProjectEntity>
{
    public void Configure(EntityTypeBuilder<ProjectEntity> builder)
    {
        builder.ToTable("Projects");

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.Metadata)
            .HasJsonConversion<ProjectData>();

        // ---------------- العلاقات ----------------

        builder.HasMany(x => x.Calculations)
            .WithOne(x => x.Project)
            .HasForeignKey(x => x.ProjectId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Folder)
            .WithMany(x => x.FolderProjects)
            .HasForeignKey(x => x.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ProcurementMethod)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.ProcurementMethodId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.ProjectType)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.ProjectTypeId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Compensation)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.CompensationId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Contract)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Organisation)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.OrganisationId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // ---------------- Constraints ----------------
        // تاريخ النهاية لا يجب أن يكون قبل البداية
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Projects_DateRange", "[EndDate] >= [StartDate]")
        );

        // Code (اختياري) لكن إذا موجود يجب أن يكون unique داخل نفس tenant
        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[Code] IS NOT NULL AND [Code] <> ''")
            .HasDatabaseName("UX_Projects_Tenant_Code");

        // ---------------- Indexes (تحسين الأداء) ----------------

        // Folder + visibility + order (ممتاز)
        builder.HasIndex(x => new { x.TenantId, x.FolderId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_Projects_Tenant_Folder_Visible_Order");

        // CreatedBy queries
        builder.HasIndex(x => new { x.TenantId, x.CreatedBy })
            .HasDatabaseName("IX_Projects_Tenant_CreatedBy");

        // ✅ إضافة مهمة إذا عندك فلترة كثيرة حسب Department
        builder.HasIndex(x => new { x.TenantId, x.DepartmentId })
            .HasDatabaseName("IX_Projects_Tenant_Department");

        // (اختياري) بحث بالاسم داخل tenant
        builder.HasIndex(x => new { x.TenantId, x.Name })
            .HasDatabaseName("IX_Projects_Tenant_Name");
    }
}
