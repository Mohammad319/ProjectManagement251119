using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class AccountGroupConfiguration : IEntityTypeConfiguration<AccountGroupEntity>
{
    public void Configure(EntityTypeBuilder<AccountGroupEntity> builder)
    {
        builder.ToTable("AccountGroups");

        // Multi-tenant safety: اسم المجموعة unique داخل نفس Tenant
        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("UX_AccountGroups_Tenant_Name");

        // جودة البيانات
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_AccountGroups_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0")
        );

        // Relationships
        builder.HasMany(x => x.Accounts)
            .WithOne(x => x.AccountGroup)
            .HasForeignKey(x => x.AccountGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AccountConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.ToTable("Accounts");

        // Multi-tenant safety: Code يجب أن يكون unique داخل نفس Tenant
        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasDatabaseName("UX_Accounts_Tenant_Code");

        // Group listing + filter
        builder.HasIndex(x => new { x.TenantId, x.AccountGroupId, x.IsVisible, x.Id })
            .HasDatabaseName("IX_Accounts_Tenant_Group_Visible_Id");

        // (اختياري) بحث بالاسم داخل tenant
        builder.HasIndex(x => new { x.TenantId, x.Name })
            .HasDatabaseName("IX_Accounts_Tenant_Name");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Accounts_Code_NotEmpty", "LEN(LTRIM(RTRIM([Code]))) > 0");
            t.HasCheckConstraint("CK_Accounts_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
        });

        builder.HasOne(x => x.AccountGroup)
            .WithMany(x => x.Accounts)
            .HasForeignKey(x => x.AccountGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
