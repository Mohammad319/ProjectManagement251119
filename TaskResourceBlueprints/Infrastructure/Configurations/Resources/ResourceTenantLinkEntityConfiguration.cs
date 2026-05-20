using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities;
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

        builder.HasIndex(x => new { x.TenantId, x.ResourceId })
            .IsUnique()
            .HasDatabaseName("UX_ResourceTenantLink_Tenant_Resource");

        builder.HasOne<ResourceDefinition>()
            .WithMany(x => x.TenantLinks)
            .HasForeignKey(x => x.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ResourceTenantLinks_Tenant_Positive", "[TenantId] > 0");
            t.HasCheckConstraint("CK_ResourceTenantLinks_Resource_Positive", "[ResourceId] > 0");
            t.HasCheckConstraint("CK_ResourceTenantLinks_Cost_NonNegative", "[Cost] IS NULL OR [Cost] >= 0");
            t.HasCheckConstraint("CK_ResourceTenantLinks_Quantity_NonNegative", "[Quantity] IS NULL OR [Quantity] >= 0");
        });
    }
}
