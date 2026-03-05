using Domain.Entities.Folder;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class FolderConfiguration : IEntityTypeConfiguration<FolderEntity>
{
    public void Configure(EntityTypeBuilder<FolderEntity> builder)
    {
        builder.ToTable("Folders");

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
        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_Folders_Tenant_Department_Visible_Order");

        // بحث بالاسم داخل القسم
        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.Name })
            .HasDatabaseName("IX_Folders_Tenant_Department_Name");

        // جودة البيانات
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Folders_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0")
        );

        // (اختياري) إذا كثير تبحث بالاسم أو تسوي فلترة بالاسم
        // builder.HasIndex(x => x.Name);

        // (اختياري) لو تبي تمنع تكرار الاسم داخل نفس القسم
        //builder.HasIndex(x => new {  x.DepartmentId, x.Name }).IsUnique();
    }
}

