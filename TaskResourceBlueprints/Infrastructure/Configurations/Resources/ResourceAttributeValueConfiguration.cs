using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Resources;

public class ResourceAttributeValueConfiguration : IEntityTypeConfiguration<ResourceAttributeValue>
{
    public void Configure(EntityTypeBuilder<ResourceAttributeValue> builder)
    {
        builder.Property(x => x.NumericValue)
            .HasPrecision(Precision.FactorPrecision, Precision.FactorScale);
    }
}
