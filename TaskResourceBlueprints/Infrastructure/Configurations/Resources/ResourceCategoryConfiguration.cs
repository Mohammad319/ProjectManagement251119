using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Resources;

public class ResourceCategoryConfiguration : IEntityTypeConfiguration<ResourceCategory>
{
    public void Configure(EntityTypeBuilder<ResourceCategory> builder)
    {
        builder.Property(x => x.DisplayName)
            .HasMaxLength(Lengths.DisplayName);

        builder.Property(x => x.Note)
            .HasMaxLength(1024);

        builder.HasIndex(x => new { x.ParentCategoryId, x.SortOrder, x.DisplayName });
    }
}
