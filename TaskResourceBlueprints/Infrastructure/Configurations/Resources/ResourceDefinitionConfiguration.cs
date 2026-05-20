using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;
using TaskResourceBlueprints.Infrastructure.Extensions;
using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Resources;

public class ResourceDefinitionConfiguration : IEntityTypeConfiguration<ResourceDefinition>
{
    public void Configure(EntityTypeBuilder<ResourceDefinition> b)
    {
        b.Property(x => x.Name)
            .HasMaxLength(Lengths.DisplayName);

        b.Property(x => x.Unit)
            .HasMaxLength(30);

        b.Property(x => x.Quantity)
            .HasPrecision(Precision.FactorPrecision, Precision.FactorScale);

        b.Property(e => e.CostRoles)
            .HasJsonListComparer<RoleDTO>();

        b.Property(e => e.Data)
            .HasJsonConversionWithComparer();


        b.HasIndex(x => new { x.FolderId, x.SortOrder, x.Name });
        b.HasIndex(x => new { x.IsActive, x.IsVisible, x.Name });

        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Resources_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_Resources_SortOrder_NonNegative", "[SortOrder] >= 0");
            t.HasCheckConstraint("CK_Resources_Quantity_NonNegative", "[Quantity] IS NULL OR [Quantity] >= 0");
        });
    }
}
