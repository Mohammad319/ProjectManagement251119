using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Questions.Assignments;
using TaskResourceBlueprints.Infrastructure.Extensions;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Assignments;

public class TaskResourceAssignmentConfiguration : IEntityTypeConfiguration<TaskResourceAssignment>
{
    public void Configure(EntityTypeBuilder<TaskResourceAssignment> b)
    {
        b.Property(e => e.CapacityRoles)
            .HasJsonListComparer();
    }
}
