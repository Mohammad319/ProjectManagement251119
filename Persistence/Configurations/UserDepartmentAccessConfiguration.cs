using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class UserDepartmentAccessConfiguration : IEntityTypeConfiguration<UserDepartmentAccessEntity>
{
    public void Configure(EntityTypeBuilder<UserDepartmentAccessEntity> builder)
    {
        builder.ToTable("UserDepartmentAccesses");

        builder.HasIndex(x => new { x.TenantId, x.UserId, x.DepartmentId })
            .IsUnique()
            .HasDatabaseName("UX_UserDepartmentAccesses_Tenant_User_Department");

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId })
            .HasDatabaseName("IX_UserDepartmentAccesses_Tenant_Department");

        builder.HasOne(x => x.User)
            .WithMany(x => x.DepartmentAccesses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Department)
            .WithMany(x => x.UserAccesses)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
