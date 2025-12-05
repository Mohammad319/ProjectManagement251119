using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Questions.Assignments;
using TaskResourceBlueprints.Infrastructure.Extensions;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Assignments;

public class NumericResourceAssignmentConfiguration : IEntityTypeConfiguration<NumericResourceAssignment>
{
    public void Configure(EntityTypeBuilder<NumericResourceAssignment> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.HasOne(x => x.Numeric)
         .WithMany(g => g.ResourceAssignments)
         .HasForeignKey(x => x.NumericId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Assignment)
         .WithMany(r => r.NumericAssignments)
         .HasForeignKey(x => x.AssignmentId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.NumericId, x.AssignmentId }).IsUnique();

        b.Property(x => x.Expressions)
            .HasJsonListComparer();
    }
}
