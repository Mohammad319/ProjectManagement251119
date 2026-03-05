using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<DepartmentEntity>
{
    public void Configure(EntityTypeBuilder<DepartmentEntity> builder)
    {
        builder.ToTable("Departments");

        // Multi-tenant safety: اسم القسم يجب أن يكون unique داخل نفس Tenant.
        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("UX_Departments_Tenant_Name");

        // جودة البيانات
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Departments_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0")
        );

        builder.HasMany(x => x.Users)
            .WithOne(u => u.Department)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Folders)
            .WithOne(f => f.Department)
            .HasForeignKey(f => f.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
