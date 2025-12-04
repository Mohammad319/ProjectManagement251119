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
            modelBuilder.Property(p => p.Created).HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Property(e => e.Data).HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
                v => JsonSerializer.Deserialize<ProjectData>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ProjectData());

            modelBuilder.HasMany(x => x.Calculations).WithOne(u => u.Project).HasForeignKey(pt => pt.ProjectId)
                .HasPrincipalKey(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.HasOne(pt => pt.Folder).WithMany(p => p.Projects).HasForeignKey(pt => pt.FolderId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.HasOne(pt => pt.ProcurementMethods).WithMany(p => p.Projects).HasForeignKey(pt => pt.ProcurementMethodsId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.User).WithMany(p => p.Projects).HasForeignKey(pt => pt.UserId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Type).WithMany(p => p.Projects).HasForeignKey(pt => pt.TypeId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Compensation).WithMany(p => p.Projects).HasForeignKey(pt => pt.CompensationId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Contract).WithMany(p => p.Projects).HasForeignKey(pt => pt.ContractId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Organisation).WithMany(p => p.Projects).HasForeignKey(pt => pt.OrganisationId).OnDelete(DeleteBehavior.SetNull);

        }
    }
}
