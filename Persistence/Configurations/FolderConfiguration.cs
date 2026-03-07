using Domain.Entities.Folder;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class FolderConfiguration : IEntityTypeConfiguration<FolderEntity>
{
    public void Configure(EntityTypeBuilder<FolderEntity> builder)
    {
        builder.ToTable("Folders", t =>
        {
            t.HasCheckConstraint("CK_Folders_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_Folders_SortOrder_NonNegative", "[SortOrder] >= 0");
            t.HasCheckConstraint("CK_Folders_Department_Positive", "[DepartmentId] > 0");
        });

        builder.HasOne(x => x.Department)
            .WithMany(x => x.Folders)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.FolderProjects)
            .WithOne(x => x.Folder)
            .HasForeignKey(x => x.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_Folders_Tenant_Department_Visible_Order");

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.Name })
            .HasDatabaseName("IX_Folders_Tenant_Department_Name");
    }
}
