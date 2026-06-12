using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class ProjectBidPriceColumnConfiguration : IEntityTypeConfiguration<ProjectBidPriceColumnEntity>
{
    public void Configure(EntityTypeBuilder<ProjectBidPriceColumnEntity> builder)
    {
        builder.ToTable("ProjectBidPriceColumns");

        builder.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.ProjectId })
            .HasDatabaseName("IX_ProjectBidPriceColumns_Tenant_Project");
    }
}
