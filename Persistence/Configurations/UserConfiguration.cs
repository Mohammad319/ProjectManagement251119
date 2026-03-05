using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        // مهم: لا تترك اسم الجدول "User" (قد يسبب مشاكل/تعارضات في SQL).
        builder.ToTable("Users");

        // -------------------------
        // Multi-tenant safety
        // -------------------------
        // Email يجب أن يكون unique داخل نفس Tenant فقط.
        builder.HasIndex(u => new { u.TenantId, u.Email })
            .IsUnique()
            .HasDatabaseName("UX_Users_Tenant_Email");

        // مفيد للبحث/تسجيل الدخول داخل tenant
        builder.HasIndex(u => new { u.TenantId, u.UserName })
            .HasDatabaseName("IX_Users_Tenant_UserName");

        // ExternalAuthId (غير unique لأن قيمته قد تكون فارغة لبعض المستخدمين)
        builder.HasIndex(u => new { u.TenantId, u.ExternalAuthId })
            .HasDatabaseName("IX_Users_Tenant_ExternalAuthId");

        // -------------------------
        // Lengths (تتماشى مع الـ DataAnnotations)
        // -------------------------
        builder.Property(u => u.FirstName).HasMaxLength(80);
        builder.Property(u => u.LastName).HasMaxLength(80);
        builder.Property(u => u.ExternalAuthId).HasMaxLength(200);

        // -------------------------
        // Quality constraints
        // -------------------------
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Users_Email_NotEmpty", "LEN(LTRIM(RTRIM([Email]))) > 0");
            t.HasCheckConstraint("CK_Users_UserName_NotEmpty", "LEN(LTRIM(RTRIM([UserName]))) > 0");
        });
    }
}
