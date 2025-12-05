using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectImportHub.Entities.Questions.Conditions;

namespace ProjectImportHub.Infrastructure.Configurations.Conditions;

public class ResourceConditionRuleConfiguration : IEntityTypeConfiguration<ResourceConditionRule>
{
    public void Configure(EntityTypeBuilder<ResourceConditionRule> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.HasOne(x => x.QuestionCondition)
         .WithMany(c => c.ResourceRules)
         .HasForeignKey(x => x.QuestionConditionId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.ResourceOptionGroup)
         .WithMany()
         .HasForeignKey(x => x.ResourceOptionGroupId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ResourceOptionItem)
         .WithMany()
         .HasForeignKey(x => x.ResourceOptionItemId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.QuestionConditionId, x.ResourceOptionGroupId, x.ResourceOptionItemId, x.SetKey }).IsUnique();
    }
}
