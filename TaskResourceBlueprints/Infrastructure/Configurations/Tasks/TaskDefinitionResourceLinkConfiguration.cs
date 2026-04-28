using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Tasks;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Tasks;

public class TaskDefinitionResourceLinkConfiguration : IEntityTypeConfiguration<TaskDefinitionResourceLink>
{
    public void Configure(EntityTypeBuilder<TaskDefinitionResourceLink> b)
    {
        b.Property(x => x.ResourceDefinitionId)
            .HasColumnName("ResourceId");

        b.Property(x => x.Quantity).HasPrecision(18, 6);

        b.HasIndex(x => new { x.TaskDefinitionId, x.ResourceDefinitionId }).IsUnique();

        b.HasOne(x => x.Task)
            .WithMany(x => x.ResourceLinks)
            .HasForeignKey(x => x.TaskDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Resource)
            .WithMany()
            .HasForeignKey(x => x.ResourceDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
