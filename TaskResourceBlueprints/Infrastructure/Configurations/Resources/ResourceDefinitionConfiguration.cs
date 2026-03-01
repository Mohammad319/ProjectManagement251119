using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;
using TaskResourceBlueprints.Infrastructure.Extensions;
using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Resources;

/// <summary>
/// Configuration for resource templates (metadata, cost, roles, etc.).
/// </summary>
public class ResourceDefinitionConfiguration : IEntityTypeConfiguration<ResourceDefinition>
{
    public void Configure(EntityTypeBuilder<ResourceDefinition> b)
    {
        // ---- Column constraints ----
        b.Property(x => x.Name)
            .HasMaxLength(Lengths.DisplayName)
            .IsRequired();

        b.Property(x => x.AdminNote)
            .HasMaxLength(Lengths.Description);

        // CostRoles: List<RoleDTO> as JSON + comparer
        b.Property(e => e.CostRoles)
            .HasJsonListComparer();

        // Metadata: ResourceMetadata as JSON + comparer
        b.Property(e => e.Data)
            .HasJsonConversionWithComparer();

        // CalcResCost: CalcResCost as JSON + comparer
        b.Property(e => e.CalcResCost)
            .HasJsonConversionWithComparer();

        // لو عندك أرقام مالية إضافية تقدر تستخدم Precision:
        // b.Attribute(e => e.SomeMoneyField)
        //     .HasPrecision(Precision.MoneyPrecision, Precision.MoneyScale);
    }
}
