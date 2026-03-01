using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Tasks;

public class TaskDefinitionConfiguration : IEntityTypeConfiguration<TaskDefinition>
{
    public void Configure(EntityTypeBuilder<TaskDefinition> builder)
    {
        // ---- Column constraints ----
        builder.Property(x => x.Name)
            .HasMaxLength(Lengths.DisplayName)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(Lengths.Code);

        builder.Property(x => x.UnitCode)
            .HasMaxLength(Lengths.Code);

        builder.Property(x => x.Responsible)
            .HasMaxLength(Lengths.DisplayName);

        builder.Property(x => x.AdminNote)
            .HasMaxLength(Lengths.Description);

        builder.Property(x => x.FieldNotes)
            .HasMaxLength(Lengths.Description);

        builder.Property(x => x.Status)
            .HasConversion<int>();

        // Task → choice question groups
        builder.HasMany(t => t.QuestionGroups)
               .WithOne(g => g.Task)
               .HasForeignKey(g => g.TaskId)
               .OnDelete(DeleteBehavior.Cascade);

        // Task → resource selector groups
        builder.HasMany(t => t.ResourceSelectors)
               .WithOne(g => g.Task)
               .HasForeignKey(g => g.TaskId)
               .OnDelete(DeleteBehavior.Cascade);

        // Task → numeric questions
        builder.HasMany(t => t.NumericQuestions)
               .WithOne(g => g.Task)
               .HasForeignKey(g => g.TaskId)
               .OnDelete(DeleteBehavior.Cascade);

        // Task → conditions
        builder.HasMany(t => t.Conditions)
               .WithOne(c => c.Task)
               .HasForeignKey(c => c.TaskId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
