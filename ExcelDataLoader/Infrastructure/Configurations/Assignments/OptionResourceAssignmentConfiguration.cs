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

        b.HasOne(x => x.ChoiceOption)
         .WithMany(o => o.OptionResourceAssignments)
         .HasForeignKey(x => x.ChoiceOptionId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.ResourceAssignment)
         .WithMany(r => r.OptionResourceFormulas)
         .HasForeignKey(x => x.ResourceAssignmentId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.ChoiceOptionId, x.ResourceAssignmentId }).IsUnique();

        b.Property(x => x.Formulas)
            .HasJsonListComparer();
    }
}
