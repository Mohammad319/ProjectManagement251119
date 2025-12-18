using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using System.Text.Json;
using static ProjectManagement.Shared.Constant.URLConst;

namespace Persistence.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<ProjectEntity>
    {
        public void Configure(EntityTypeBuilder<ProjectEntity> modelBuilder)
        {
            modelBuilder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Property(e => e.Metadata).HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
                v => JsonSerializer.Deserialize<ProjectData>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ProjectData());

            modelBuilder.HasMany(x => x.Calculations).WithOne(u => u.Project).HasForeignKey(pt => pt.ProjectId)
                .HasPrincipalKey(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.HasOne(pt => pt.Folder).WithMany(p => p.FolderProjects).HasForeignKey(pt => pt.FolderId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.HasOne(pt => pt.ProcurementMethod).WithMany(p => p.Projects).HasForeignKey(pt => pt.ProcurementMethodId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.ProjectType).WithMany(p => p.Projects).HasForeignKey(pt => pt.ProjectTypeId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Compensation).WithMany(p => p.Projects).HasForeignKey(pt => pt.CompensationId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Contract).WithMany(p => p.Projects).HasForeignKey(pt => pt.ContractId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Organisation).WithMany(p => p.Projects).HasForeignKey(pt => pt.OrganisationId).OnDelete(DeleteBehavior.SetNull);

        }
    }
}
