using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    class DepartmentConfiguration : IEntityTypeConfiguration<DepartmentEntity>
    {
        public void Configure(EntityTypeBuilder<DepartmentEntity> modelBuilder)
        {
            modelBuilder.HasMany(x => x.Users).WithOne(u => u.Department)
                .HasForeignKey(pt => pt.DepartmentId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.HasMany(x => x.Folders).WithOne(u => u.Department)
                .HasForeignKey(pt => pt.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
