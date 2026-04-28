using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Lookups;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Tasks;

public class TaskDefinitionStateLinkConfiguration : IEntityTypeConfiguration<TaskDefinitionStateLink>
{
    public void Configure(EntityTypeBuilder<TaskDefinitionStateLink> b)
    {
        b.HasIndex(x => new { x.TaskDefinitionId, x.TaskStateId }).IsUnique();

        b.HasOne(x => x.Task)
         .WithMany(x => x.StateLinks)
         .HasForeignKey(x => x.TaskDefinitionId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.State)
         .WithMany(x => x.TaskLinks)
         .HasForeignKey(x => x.TaskStateId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
