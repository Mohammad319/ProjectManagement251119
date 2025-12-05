using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectImportHub.Entities.Questions.Assignments;
using ProjectImportHub.Infrastructure.Extensions;

namespace ProjectImportHub.Infrastructure.Configurations.Assignments;

public class OptionResourceAssignmentConfiguration : IEntityTypeConfiguration<OptionResourceAssignment>
{
    public void Configure(EntityTypeBuilder<OptionResourceAssignment> b)
    {
        b.HasKey(x => x.Id);

        b.HasOne(x => x.Option)
         .WithMany(o => o.OptionResourceAssignments)
         .HasForeignKey(x => x.OptionId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Assignment)
         .WithMany(r => r.OptionAssignments)
         .HasForeignKey(x => x.AssignmentId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.OptionId, x.AssignmentId }).IsUnique();

        b.Property(x => x.Expressions)
            .HasJsonListComparer();
    }
}
