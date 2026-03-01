using Domain.Entities.Folder;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class FolderConfiguration : IEntityTypeConfiguration<FolderEntity>
{
    public void Configure(EntityTypeBuilder<FolderEntity> builder)
    {
        // العلاقات
        builder.HasOne(x => x.Department)
            .WithMany(x => x.Folders)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.FolderProjects)
            .WithOne(x => x.Folder)
            .HasForeignKey(x => x.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---------------- Indexes ----------------
        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.IsVisible, x.SortOrder });

        // منع تكرار نفس اسم المجلد داخل نفس القسم والتينانت
        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.Name })
            .IsUnique()
            .HasDatabaseName("UX_Folders_Tenant_Department_Name");

        // (اختياري) إذا كثير تبحث بالاسم أو تسوي فلترة بالاسم
        // builder.HasIndex(x => x.Name);

        // (اختياري) لو تبي تمنع تكرار الاسم داخل نفس القسم
        //builder.HasIndex(x => new {  x.DepartmentId, x.Name }).IsUnique();
    }
}

