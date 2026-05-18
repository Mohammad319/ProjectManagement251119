using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Shared.Base.Calculation;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;
using TaskResourceBlueprints.Infrastructure.Extensions;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Tasks;

public class TaskDefinitionConfiguration : IEntityTypeConfiguration<TaskDefinition>
{
    public void Configure(EntityTypeBuilder<TaskDefinition> builder)
    {
        builder.Property(x => x.Name)
            .HasMaxLength(Lengths.DisplayName);

        builder.Property(x => x.Code)
            .HasMaxLength(Lengths.DisplayName);

        builder.Property(x => x.ParentCode)
            .HasMaxLength(Lengths.DisplayName);

        builder.Property(x => x.ParentName)
            .HasMaxLength(Lengths.DisplayName);

        builder.Property(x => x.HierarchyPath)
            .HasMaxLength(1024);

        builder.Property(x => x.NormalizedTextSv)
            .HasMaxLength(Lengths.NormalizedText);

        builder.Property(x => x.UnitCode)
            .HasMaxLength(64);

        builder.Property(x => x.Quantity)
            .HasPrecision(Precision.FactorPrecision, Precision.FactorScale);

        builder.Property(x => x.PriceProduction)
            .HasPrecision(Precision.FactorPrecision, Precision.FactorScale);

        builder.Property(x => x.ChangeFactor1)
            .HasPrecision(Precision.FactorPrecision, Precision.FactorScale);

        builder.Property(x => x.ChangeFactor2)
            .HasPrecision(Precision.FactorPrecision, Precision.FactorScale);

        builder.Property(x => x.ConversionParameters)
            .HasJsonListComparer();

        builder.Property(x => x.RowNotes)
            .HasJsonListComparer();

        builder.Property(x => x.NameSynonyms)
            .HasJsonListComparer();

        builder.Property(x => x.UnitSynonyms)
            .HasJsonListComparer();

        builder.Property(x => x.VisibleFolderIds)
            .HasJsonScalarListComparer();

        builder.Property(x => x.WorkloadThresholds)
            .HasJsonScalarListComparer();

        builder.HasIndex(x => new { x.Status, x.SortOrder });
        builder.HasIndex(x => x.NormalizedTextSv);
        builder.HasIndex(x => x.Code);
        builder.HasIndex(x => x.ParentCode);
    }
}
