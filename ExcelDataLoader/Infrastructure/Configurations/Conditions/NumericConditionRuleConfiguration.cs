using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Questions.Conditions;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Conditions;

public class NumericConditionRuleConfiguration : IEntityTypeConfiguration<NumericConditionRule>
{
    public void Configure(EntityTypeBuilder<NumericConditionRule> b)
    {
        b.HasOne(x => x.Condition)
         .WithMany(c => c.NumericRules)
         .HasForeignKey(x => x.ConditionId);

        b.HasOne(x => x.NumericQuestion)
         .WithMany()
         .HasForeignKey(x => x.NumericQuestionId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.ConditionId, x.GroupKey });
    }
}
