using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectImportHub.Entities.Questions.Assignments;
using ProjectImportHub.Infrastructure.Extensions;

namespace ProjectImportHub.Infrastructure.Configurations.Assignments;

public class TaskResourceAssignmentConfiguration : IEntityTypeConfiguration<TaskResourceAssignment>
{
    public void Configure(EntityTypeBuilder<TaskResourceAssignment> b)
    {
        b.Property(e => e.CapacityRoles)
            .HasJsonListComparer();
    }
}
