using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Questions.Conditions;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Conditions;

public class ResourceConditionRuleConfiguration : IEntityTypeConfiguration<ResourceConditionRule>
{
    public void Configure(EntityTypeBuilder<ResourceConditionRule> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.HasOne(x => x.Condition)
         .WithMany(c => c.ResourceRules)
         .HasForeignKey(x => x.ConditionId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Selector)
         .WithMany()
         .HasForeignKey(x => x.SelectorId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SelectorItem)
         .WithMany()
         .HasForeignKey(x => x.SelectorItemId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.ConditionId, x.SelectorId, x.SelectorItemId, x.GroupKey }).IsUnique();
    }
}
