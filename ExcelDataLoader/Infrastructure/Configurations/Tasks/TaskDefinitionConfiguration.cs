using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectImportHub.Entities.Tasks;

namespace ProjectImportHub.Infrastructure.Configurations.Tasks;

public class TaskDefinitionConfiguration : IEntityTypeConfiguration<TaskDefinition>
{
    public void Configure(EntityTypeBuilder<TaskDefinition> builder)
    {
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
