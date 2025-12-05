using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Questions.Conditions;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Conditions;

public class ConditionDefinitionConfiguration : IEntityTypeConfiguration<ConditionDefinition>
{
    public void Configure(EntityTypeBuilder<ConditionDefinition> b)
    {
        b.Property(x => x.OptionResourceLogic).HasConversion<int>();
        b.Property(x => x.OptionNumericLogic).HasConversion<int>();
        b.Property(x => x.NumericResourceLogic).HasConversion<int>();
    }
}
