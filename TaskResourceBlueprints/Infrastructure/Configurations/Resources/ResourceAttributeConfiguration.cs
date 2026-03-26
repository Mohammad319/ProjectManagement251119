using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Resources;

public class ResourceAttributeConfiguration : IEntityTypeConfiguration<ResourceAttribute>
{
    public void Configure(EntityTypeBuilder<ResourceAttribute> builder)
    {
        builder.Property(x => x.DefaultNumericValue)
            .HasPrecision(Precision.FactorPrecision, Precision.FactorScale);

        builder.Property(x => x.StepValue)
            .HasPrecision(Precision.FactorPrecision, Precision.FactorScale);

        builder.Property(x => x.MaxNumericValue)
            .HasPrecision(Precision.FactorPrecision, Precision.FactorScale);
    }
}
