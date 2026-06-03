using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Serialization;
using ProjectManagement.Shared.DTO.Project;

namespace Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<ProjectEntity>
{
    public void Configure(EntityTypeBuilder<ProjectEntity> builder)
    {
        builder.ToTable("Projects", t =>
        {
            t.HasCheckConstraint("CK_Projects_DateRange", "[EndDate] >= [StartDate]");
            t.HasCheckConstraint("CK_Projects_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_Projects_SortOrder_NonNegative", "[SortOrder] >= 0");
        });

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.Metadata)
            .HasJsonConversion<ProjectData>();

        builder.HasMany(x => x.Calculations)
            .WithOne(x => x.Project)
            .HasForeignKey(x => x.ProjectId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Folder)
            .WithMany(x => x.FolderProjects)
            .HasForeignKey(x => x.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ProcurementMethod)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.ProcurementMethodId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.ProjectType)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.ProjectTypeId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.ProjectStatus)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.ProjectStatusId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Compensation)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.CompensationId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Contract)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Organisation)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.OrganisationId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [Code] IS NOT NULL AND [Code] <> ''")
            .HasDatabaseName("UX_Projects_Tenant_Code");

        builder.HasIndex(x => new { x.TenantId, x.FolderId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_Projects_Tenant_Folder_Visible_Order");

        builder.HasIndex(x => new { x.TenantId, x.CreatedBy })
            .HasDatabaseName("IX_Projects_Tenant_CreatedBy");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .HasDatabaseName("IX_Projects_Tenant_Name");
    }
}
