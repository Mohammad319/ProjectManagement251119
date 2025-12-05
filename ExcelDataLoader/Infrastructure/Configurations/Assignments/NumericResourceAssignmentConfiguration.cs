using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectImportHub.Entities.Questions.Assignments;
using ProjectImportHub.Infrastructure.Extensions;

namespace ProjectImportHub.Infrastructure.Configurations.Assignments;

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

        b.HasOne(x => x.ResourceAssignment)
         .WithMany(r => r.NumericResourceFormulas)
         .HasForeignKey(x => x.ResourceAssignmentId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.NumericId, x.ResourceAssignmentId }).IsUnique();

        b.Property(x => x.Formulas)
            .HasJsonListComparer();
    }
}
