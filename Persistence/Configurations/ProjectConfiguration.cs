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
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ProjectType)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.ProjectTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Compensation)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.CompensationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Contract)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Organisation)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.OrganisationId)
            .OnDelete(DeleteBehavior.SetNull);

        // ---------------- Indexes (تحسين الأداء) ----------------

        // يغطي أغلب استعلامات: GetByFolder + IsVisible + OrderBy SortOrder
        builder.HasIndex(x => new { x.TenantId, x.FolderId, x.IsVisible, x.SortOrder });

        // لو عندك استعلامات تجيب مشاريع مستخدم معيّن (أو داخل CreateAsync عندك فلترة CreatedBy)
        builder.HasIndex(x => new { x.TenantId, x.CreatedBy });

        // (اختياري) إذا عندك ترتيب عام داخل التينانت بدون FolderId
        // builder.HasIndex(x => new { x.TenantId, x.SortOrder });

        // تحسين أداء شائع في SaaS (اختياري) لو TenantId موجود على ProjectEntity:
        // builder.HasIndex(x => new { x.TenantId, x.Id });
    }
}
