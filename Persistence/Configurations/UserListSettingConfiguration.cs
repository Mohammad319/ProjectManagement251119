using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class UserListSettingConfiguration : IEntityTypeConfiguration<UserListSettingEntity>
{
    public void Configure(EntityTypeBuilder<UserListSettingEntity> builder)
    {
        builder.ToTable("UserListSettings");

        // One row per user/scope/kind within a tenant. The unique index also
        // serves the lookup used on every load/upsert.
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.Scope, x.Kind })
            .IsUnique()
            .HasDatabaseName("UX_UserListSettings_Tenant_User_Scope_Kind");

        builder.Property(x => x.Scope).HasMaxLength(60);
        builder.Property(x => x.Kind).HasMaxLength(60);
        builder.Property(x => x.Payload).IsRequired();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_UserListSettings_Scope_NotEmpty", "LEN(LTRIM(RTRIM([Scope]))) > 0");
            t.HasCheckConstraint("CK_UserListSettings_Kind_NotEmpty", "LEN(LTRIM(RTRIM([Kind]))) > 0");
        });
    }
}
