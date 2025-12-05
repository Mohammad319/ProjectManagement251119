using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Resources;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Resources;

public class ResourceAttributeConfiguration : IEntityTypeConfiguration<ResourceAttribute>
{
    public void Configure(EntityTypeBuilder<ResourceAttribute> builder)
    {
        // أضِف أي MaxLength/Indexes هنا إذا لزم لاحقًا
    }
}
