using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class ProjectBidConfiguration : IEntityTypeConfiguration<ProjectBidEntity>
{
    public void Configure(EntityTypeBuilder<ProjectBidEntity> builder)
    {
        builder.ToTable("ProjectBids");

        builder.HasOne(x => x.Project)
            .WithMany(x => x.Bids)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.ProjectId })
            .HasDatabaseName("IX_ProjectBids_Tenant_Project");
    }
}
