using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class UserManagementAuditConfiguration : IEntityTypeConfiguration<UserManagementAuditEntity>
{
    public void Configure(EntityTypeBuilder<UserManagementAuditEntity> builder)
    {
        builder.ToTable("UserManagementAuditLogs");
        builder.Property(x => x.Action).HasMaxLength(60).IsRequired();
        builder.Property(x => x.TargetAuthId).HasMaxLength(450);
        builder.Property(x => x.TargetDisplayName).HasMaxLength(240).IsRequired();
        builder.Property(x => x.ActorDisplayName).HasMaxLength(240).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc })
            .HasDatabaseName("IX_UserManagementAuditLogs_Tenant_CreatedAt");
        builder.HasIndex(x => new { x.TenantId, x.TargetUserId })
            .HasDatabaseName("IX_UserManagementAuditLogs_Tenant_TargetUser");
    }
}
