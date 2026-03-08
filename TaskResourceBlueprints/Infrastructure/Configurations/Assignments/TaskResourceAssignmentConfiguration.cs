using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Questions.Assignments;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;
using TaskResourceBlueprints.Infrastructure.Extensions;
using ProjectManagement.Shared.Base.AppTenant;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Assignments;

public class TaskResourceAssignmentConfiguration : IEntityTypeConfiguration<TaskResourceAssignment>
{
    public void Configure(EntityTypeBuilder<TaskResourceAssignment> b)
    {
        b.Property(x => x.BaseCost).HasPrecision(Precision.MoneyPrecision, Precision.MoneyScale);
        b.Property(x => x.ChangeFactor1).HasPrecision(Precision.FactorPrecision, Precision.FactorScale);
        b.Property(x => x.ChangeFactor2).HasPrecision(Precision.FactorPrecision, Precision.FactorScale);
        b.Property(x => x.CapWaste).HasPrecision(Precision.FactorPrecision, Precision.FactorScale);

        b.Property(e => e.CapacityRoles)
            .HasJsonListComparer<RoleDTO>();

        b.Property(e => e.Expressions)
            .HasJsonListComparer();

        b.HasIndex(x => new { x.TaskId, x.ResourceId })
            .IsUnique();
    }
}
