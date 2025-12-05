using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectImportHub.Entities.Questions.Conditions;

namespace ProjectImportHub.Infrastructure.Configurations.Conditions;

public class OptionConditionRuleConfiguration : IEntityTypeConfiguration<OptionConditionRule>
{
    public void Configure(EntityTypeBuilder<OptionConditionRule> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.HasOne(x => x.Condition)
         .WithMany(c => c.OptionRules)
         .HasForeignKey(x => x.ConditionId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.QuestionGroup)
         .WithMany()
         .HasForeignKey(x => x.QuestionGroupId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Option)
         .WithMany()
         .HasForeignKey(x => x.OptionId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.ConditionId, x.QuestionGroupId, x.OptionId, x.GroupKey }).IsUnique();
    }
}
