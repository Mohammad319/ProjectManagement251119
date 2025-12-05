using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectImportHub.Entities.Questions.Assignments;
using ProjectImportHub.Infrastructure.ConfigurationConstants;
using ProjectImportHub.Infrastructure.Extensions;
using ProjectManagement.Shared.Base.AppTenant;

namespace ProjectImportHub.Infrastructure.Configurations.Assignments;

public class ConditionResourceAssignmentConfiguration : IEntityTypeConfiguration<ConditionResourceAssignment>
{
    public void Configure(EntityTypeBuilder<ConditionResourceAssignment> b)
    {
        b.Property(e => e.CapRole)
            .HasJsonListComparer<RoleDTO>();

        b.Property(x => x.BaseCost).HasPrecision(Precision.MoneyPrecision, Precision.MoneyScale);
        b.Property(x => x.ChangeFactor1).HasPrecision(Precision.FactorPrecision, Precision.FactorScale);
        b.Property(x => x.ChangeFactor2).HasPrecision(Precision.FactorPrecision, Precision.FactorScale);
        b.Property(x => x.CapWaste).HasPrecision(Precision.FactorPrecision, Precision.FactorScale);
    }
}
