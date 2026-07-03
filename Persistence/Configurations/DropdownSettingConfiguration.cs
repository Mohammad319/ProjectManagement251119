using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class DropdownSettingConfiguration : IEntityTypeConfiguration<DropdownSettingEntity>
{
    public void Configure(EntityTypeBuilder<DropdownSettingEntity> builder)
    {
        builder.ToTable("DropdownSettings");

        // One row per dropdown category within a tenant. The unique index also
        // serves the lookup used on every load/upsert.
        builder.HasIndex(x => new { x.TenantId, x.Category })
            .IsUnique()
            .HasDatabaseName("UX_DropdownSettings_Tenant_Category");

        builder.Property(x => x.Category).HasMaxLength(60);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_DropdownSettings_Category_NotEmpty", "LEN(LTRIM(RTRIM([Category]))) > 0");
        });
    }
}
