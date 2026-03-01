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

        // (اختياري لكن مفيد قبل الإطلاق) منع تكرار Code داخل نفس التينانت
        // Code عندك nullable => لازم فلتر حتى يسمح بأكثر من NULL
        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[Code] IS NOT NULL")
            .HasDatabaseName("UX_Projects_Tenant_Code");
    }
}
