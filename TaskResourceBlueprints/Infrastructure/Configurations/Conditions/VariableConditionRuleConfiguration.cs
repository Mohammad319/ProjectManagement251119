using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Questions.Conditions;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Conditions;

public class VariableConditionRuleConfiguration : IEntityTypeConfiguration<VariableConditionRule>
{
    public void Configure(EntityTypeBuilder<VariableConditionRule> builder)
    {
        builder.Property(x => x.MinAllowedValue)
            .HasPrecision(Precision.FactorPrecision, Precision.FactorScale);

        builder.Property(x => x.MaxAllowedValue)
            .HasPrecision(Precision.FactorPrecision, Precision.FactorScale);
    }
}
