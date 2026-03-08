using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

        builder.Property(x => x.Quantity)
            .HasPrecision(Precision.FactorPrecision, Precision.FactorScale);

        builder.Property(x => x.RowNotes)
            .HasJsonListComparer();

        builder.Property(x => x.VisibleFolderIds)
            .HasJsonScalarListComparer();

        builder.Property(x => x.WorkloadThresholds)
            .HasJsonScalarListComparer();

        builder.HasIndex(x => new { x.Status, x.SortOrder });
        builder.HasIndex(x => new { x.ActionId, x.LocationId, x.FallId, x.ActionTypeId });

        builder.HasMany(t => t.QuestionGroups)
               .WithOne(g => g.Task)
               .HasForeignKey(g => g.TaskId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.ResourceSelectors)
               .WithOne(g => g.Task)
               .HasForeignKey(g => g.TaskId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.NumericQuestions)
               .WithOne(g => g.Task)
               .HasForeignKey(g => g.TaskId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Conditions)
               .WithOne(c => c.Task)
               .HasForeignKey(c => c.TaskId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
