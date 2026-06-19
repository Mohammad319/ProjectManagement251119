using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class ProjectShareConfiguration : IEntityTypeConfiguration<ProjectShareEntity>
{
    public void Configure(EntityTypeBuilder<ProjectShareEntity> builder)
    {
        builder.ToTable("ProjectShares");

        builder.Property(x => x.Role).HasMaxLength(64).IsRequired();

        // Exakt en mottagare: användare ELLER avdelning.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ProjectShares_OneRecipient",
            "([SharedWithUserId] IS NOT NULL AND [DepartmentId] IS NULL) OR ([SharedWithUserId] IS NULL AND [DepartmentId] IS NOT NULL)"));

        builder.HasIndex(x => new { x.TenantId, x.ProjectId })
            .HasDatabaseName("IX_ProjectShares_Tenant_Project");

        // En användare/avdelning delas bara en gång per projekt.
        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.SharedWithUserId })
            .IsUnique()
            .HasFilter("[SharedWithUserId] IS NOT NULL")
            .HasDatabaseName("UX_ProjectShares_Project_User");

        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.DepartmentId })
            .IsUnique()
            .HasFilter("[DepartmentId] IS NOT NULL")
            .HasDatabaseName("UX_ProjectShares_Project_Department");

        builder.HasOne(x => x.Project)
            .WithMany(p => p.Shares)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SharedWithUser)
            .WithMany()
            .HasForeignKey(x => x.SharedWithUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Calculations)
            .WithOne(x => x.ProjectShare)
            .HasForeignKey(x => x.ProjectShareId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProjectShareCalculationConfiguration : IEntityTypeConfiguration<ProjectShareCalculationEntity>
{
    public void Configure(EntityTypeBuilder<ProjectShareCalculationEntity> builder)
    {
        builder.ToTable("ProjectShareCalculations");

        builder.HasIndex(x => new { x.TenantId, x.ProjectShareId, x.CalculationId })
            .IsUnique()
            .HasDatabaseName("UX_ProjectShareCalculations_Share_Calc");

        // Restrict så att en kalkyl som ingår i en delning inte kan hård-raderas
        // utan att länken först tas bort (undviker dessutom multipla cascade-vägar).
        builder.HasOne(x => x.Calculation)
            .WithMany()
            .HasForeignKey(x => x.CalculationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
