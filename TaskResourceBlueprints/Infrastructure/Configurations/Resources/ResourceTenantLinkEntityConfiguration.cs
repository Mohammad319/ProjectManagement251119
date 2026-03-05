using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Resources;

public class ResourceTenantLinkEntityConfiguration : IEntityTypeConfiguration<ResourceTenantLinkEntity>
{
    public void Configure(EntityTypeBuilder<ResourceTenantLinkEntity> builder)
    {
        builder.Property(x => x.Cost)
            .HasPrecision(Precision.MoneyPrecision, Precision.MoneyScale);

        builder.Property(x => x.Quantity)
            .HasPrecision(Precision.FactorPrecision, Precision.FactorScale);
    }
}
