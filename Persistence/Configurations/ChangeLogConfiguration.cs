using Domain.Entities.ChangeLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class ChangeLogConfiguration : IEntityTypeConfiguration<ChangeLogEntity>
{
    public void Configure(EntityTypeBuilder<ChangeLogEntity> builder)
    {
        builder.ToTable("ChangeLogs");

        builder.Property(x => x.Action).HasConversion<int>();
        builder.Property(x => x.ActorName).HasMaxLength(200);

        // Primary read pattern: "the latest N changes for these project/calculation ids".
        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.CreatedAt })
            .HasDatabaseName("IX_ChangeLogs_Tenant_Project_Created");

        builder.HasIndex(x => new { x.TenantId, x.CalculationId, x.CreatedAt })
            .HasDatabaseName("IX_ChangeLogs_Tenant_Calculation_Created");

        // Project/Calculation are referenced by id only (no navigation/FK) so a change-log row
        // survives soft-deletes and never adds restrict-FKs. Actor is the audit CreatedBy relation
        // configured globally in ConfigureAuditUserRelations.
    }
}
