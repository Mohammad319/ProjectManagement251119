using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectImportHub.Entities.Questions.Conditions;

namespace ProjectImportHub.Infrastructure.Configurations.Conditions;

public class NumericConditionRuleConfiguration : IEntityTypeConfiguration<NumericConditionRule>
{
    public void Configure(EntityTypeBuilder<NumericConditionRule> b)
    {
        b.HasOne(x => x.QuestionCondition)
         .WithMany(c => c.NumericRules)
         .HasForeignKey(x => x.QuestionConditionId);

        b.HasOne(x => x.NumericInput)
         .WithMany()
         .HasForeignKey(x => x.NumericInputId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.QuestionConditionId, x.SetKey });
    }
}
