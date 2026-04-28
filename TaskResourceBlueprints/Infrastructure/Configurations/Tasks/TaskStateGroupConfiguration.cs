using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Lookups;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Tasks;

public class TaskStateGroupConfiguration : IEntityTypeConfiguration<TaskStateGroup>
{
    public void Configure(EntityTypeBuilder<TaskStateGroup> b)
    {
        b.Property(x => x.Name).HasMaxLength(Lengths.DisplayName);

        b.HasMany(x => x.States)
         .WithOne(x => x.Group)
         .HasForeignKey(x => x.TaskStateGroupId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
