using Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.ToTable("Notifications");

        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.ProjectName).HasMaxLength(256);
        builder.Property(x => x.ActorName).HasMaxLength(200);
        builder.Property(x => x.DepartmentName).HasMaxLength(200);
        builder.Property(x => x.Role).HasMaxLength(64);
        builder.Property(x => x.ValidUntil).HasColumnType("date");

        // Primary access pattern: "the current user's notifications, unread first, newest first".
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.ReadAt })
            .HasDatabaseName("IX_Notifications_Tenant_User_Read");

        builder.HasIndex(x => new { x.TenantId, x.UserId, x.CreatedAt })
            .HasDatabaseName("IX_Notifications_Tenant_User_Created");

        // Recipient is referenced by id only (no navigation) to avoid additional restrict-FKs to
        // UserEntity beyond the audit relations configured globally in ConfigureAuditUserRelations.
    }
}
