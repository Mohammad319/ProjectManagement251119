using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Shared.Constant;

namespace Persistence.Configurations;

internal sealed class TemplateConfiguration : IEntityTypeConfiguration<TemplateEntity>
{
    public void Configure(EntityTypeBuilder<TemplateEntity> builder)
    {
        builder.ToTable("Templates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(FieldLengths.Name);

        builder.Property(x => x.IsVisible).IsRequired();
        builder.Property(x => x.DepartmentId);

        // -------------------------
        // Indexes (SQL Server)
        // -------------------------

        // 1) قائمة Templates حسب Department داخل Tenant مع ترتيب Id Desc
        // SQL Server يستفيد من (TenantId, DepartmentId, Id) للاستعلامات + OrderBy(Id desc)
        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.Id });

        // 2) البحث/الفلترة بالاسم داخل Tenant (اختياري حسب الاستخدام)
        builder.HasIndex(x => new { x.TenantId, x.Name });

        // 3) (اختياري قوي) Unique اسم داخل Tenant+Department إذا مطلوب
        // builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.Name }).IsUnique();

        // -------------------------
        // Relationships
        // -------------------------
        builder.HasOne(x => x.Department)
               .WithMany()
               .HasForeignKey(x => x.DepartmentId)
               .OnDelete(DeleteBehavior.Restrict);

        // -------------------------
        // (اختياري) Soft “business” constraint لتحسين جودة البيانات
        // -------------------------
        // مثال: منع الاسم الفاضي بعد Trim (SQL Server يدعم check constraint)
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Templates_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0")
        );
    }
}
