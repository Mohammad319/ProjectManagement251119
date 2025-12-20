using Domain.Entities.Calculation;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.FirstName).HasMaxLength(30);
        builder.Property(u => u.LastName).HasMaxLength(30);
    }
}

internal sealed class TenderConfiguration : IEntityTypeConfiguration<TenderEntity>
{
    public void Configure(EntityTypeBuilder<TenderEntity> builder)
    {
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
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TenderAttributeBindConfiguration : IEntityTypeConfiguration<TenderAttributeBindEntity>
{
    public void Configure(EntityTypeBuilder<TenderAttributeBindEntity> builder)
    {
        builder.HasKey(m => new { m.TenderAttributeId, m.TenderId });

        builder.HasOne(x => x.Tender)
            .WithMany(x => x.TendersAttributes)
            .HasForeignKey(x => x.TenderId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TenderAttribute)
            .WithMany(x => x.TendersAttributes)
            .HasForeignKey(x => x.TenderAttributeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AttributeNameTenderConfiguration : IEntityTypeConfiguration<TenderAttributeDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<TenderAttributeDefinitionEntity> builder)
    {
        builder.HasOne(x => x.Calculation)
            .WithMany(x => x.AttributesTender)
            .HasForeignKey(x => x.CalculationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TendersAttributes)
            .WithOne(x => x.TenderAttribute)
            .HasForeignKey(x => x.TenderAttributeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
