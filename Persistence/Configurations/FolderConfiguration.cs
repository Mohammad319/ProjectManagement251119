using Domain.Entities.Folder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    class FolderConfiguration : IEntityTypeConfiguration<FolderEntity>
    {
        public void Configure(EntityTypeBuilder<FolderEntity> modelBuilder)
        {
            modelBuilder.HasOne(pt => pt.User).WithMany(p => p.Folders)
            .HasForeignKey(pt => pt.UserId).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.HasOne(pt => pt.Department).WithMany(p => p.Folders)
            .HasForeignKey(pt => pt.DepartmentId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.HasMany(x => x.FolderProjects).WithOne(u => u.Folder)
                .HasForeignKey(pt => pt.FolderId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
