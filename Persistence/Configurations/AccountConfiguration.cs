using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Serialization;

namespace Persistence.Configurations;

/// <summary>
/// DB constraints + indexes for Accounts and AccountGroups.
/// هدفها الأساسي: منع التكرار + تحسين الاستعلامات قبل الإطلاق.
/// </summary>
internal sealed class AccountGroupConfiguration : IEntityTypeConfiguration<AccountGroupEntity>
{
    public void Configure(EntityTypeBuilder<AccountGroupEntity> builder)
    {
        builder.ToTable("AccountGroup");

        // منع تكرار نفس اسم المجموعة داخل نفس التينانت
        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("UX_AccountGroup_Tenant_Name");
    }
}

internal sealed class AccountConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.ToTable("Accounts");

        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        // ✅ غالبًا الـ Code هو المفتاح الذي تستخدمه في التقارير/التصدير
        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasDatabaseName("UX_Accounts_Tenant_Code");

        // مفيد في شاشة الحسابات (فرز/بحث حسب الاسم)
        builder.HasIndex(x => new { x.TenantId, x.Name })
            .HasDatabaseName("IX_Accounts_Tenant_Name");
    }
}
