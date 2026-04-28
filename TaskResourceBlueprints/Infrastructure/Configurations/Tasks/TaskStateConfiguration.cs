using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Lookups;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Tasks;

public class TaskStateConfiguration : IEntityTypeConfiguration<TaskState>
{
    public void Configure(EntityTypeBuilder<TaskState> b)
    {
        b.Property(x => x.Name).HasMaxLength(Lengths.DisplayName);
        b.HasIndex(x => x.TaskStateGroupId);
    }
}
