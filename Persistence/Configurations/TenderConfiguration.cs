using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class TenderConfiguration : IEntityTypeConfiguration<TenderEntity>
{
    public void Configure(EntityTypeBuilder<TenderEntity> builder)
    {
        builder.ToTable("Tenders");

        builder.HasOne(x => x.Calculation)
            .WithMany(x => x.Tenders)
            .HasForeignKey(x => x.CalculationId)
            .OnDelete(DeleteBehavior.Cascade);

        // تنبيه: Cascade على Organisation غالباً خطير (يحذف كل tenders عند حذف Organisation)
        // الأفضل Restrict (عدله حسب منطق النظام عندك)
        builder.HasOne(x => x.Organisation)
            .WithMany(x => x.Tenders)
            .HasForeignKey(x => x.OrganisationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.TendersAttributes)
            .WithOne(x => x.Tender)
            .HasForeignKey(x => x.TenderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes (Tenant filter + الاستعلامات الشائعة)
        builder.HasIndex(x => new { x.TenantId, x.CalculationId })
            .HasDatabaseName("IX_Tenders_Tenant_Calc");

        builder.HasIndex(x => new { x.TenantId, x.OrganisationId })
            .HasDatabaseName("IX_Tenders_Tenant_Org");
    }
}

internal sealed class TenderAttributeBindConfiguration : IEntityTypeConfiguration<TenderAttributeBindEntity>
{
    public void Configure(EntityTypeBuilder<TenderAttributeBindEntity> builder)
    {
        builder.ToTable("TenderAttributeBinds");

        // Use Id as PK (Identity) to avoid a useless non-identity Id column when using a composite PK.
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.HasOne(x => x.Tender)
            .WithMany(x => x.TendersAttributes)
            .HasForeignKey(x => x.TenderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TenderAttribute)
            .WithMany(x => x.TendersAttributes)
            .HasForeignKey(x => x.TenderAttributeId)
            .OnDelete(DeleteBehavior.NoAction);

        // Prevent duplicates inside the same tenant (and keep a good seek pattern for Tenant+Tender queries).
        builder.HasIndex(x => new { x.TenantId, x.TenderId, x.TenderAttributeId })
            .IsUnique()
            .HasDatabaseName("UX_TenderAttributeBinds_Tenant_Tender_Attr");
    }
}

internal sealed class AttributeNameTenderConfiguration : IEntityTypeConfiguration<TenderAttributeDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<TenderAttributeDefinitionEntity> builder)
    {
        builder.ToTable("TenderAttributeDefinitions");

        builder.HasOne(x => x.Calculation)
            .WithMany(x => x.AttributesTender)
            .HasForeignKey(x => x.CalculationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TendersAttributes)
            .WithOne(x => x.TenderAttribute)
            .HasForeignKey(x => x.TenderAttributeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.CalculationId })
            .HasDatabaseName("IX_TenderAttrDefs_Tenant_Calc");
    }
}
