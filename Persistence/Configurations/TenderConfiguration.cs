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

        // Cascade على Organisation قد يحذف عطاءات كثيرة بشكل غير مقصود.
        builder.HasOne(x => x.Organisation)
            .WithMany(x => x.Tenders)
            .HasForeignKey(x => x.OrganisationId)
            .OnDelete(DeleteBehavior.Restrict);

        // العلاقة مع TenderAttributeBindEntity تُضبط من جهة الـ bind configuration
        // لتجنب تعريفها أكثر من مرة بمفاتيح / OnDelete مختلفة.

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

        // Id هو الـ PK الرسمي. منع التعريف المركب هنا مهم لأن DbContext كان يعرّفه سابقاً بشكل مختلف.
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.HasOne(x => x.Tender)
            .WithMany(x => x.TendersAttributes)
            .HasForeignKey(x => x.TenderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TenderAttribute)
            .WithMany(x => x.TendersAttributes)
            .HasForeignKey(x => x.TenderAttributeId)
            .OnDelete(DeleteBehavior.Restrict);

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

        // العلاقة مع TenderAttributeBindEntity تضبط من جهة الـ bind configuration فقط.

        builder.HasIndex(x => new { x.TenantId, x.CalculationId })
            .HasDatabaseName("IX_TenderAttrDefs_Tenant_Calc");
    }
}
