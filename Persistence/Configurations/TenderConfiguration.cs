using Domain.Entities.Calculation;
using Domain.Entities.ResourceType;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Domain.Entities.Users;


namespace Persistence.Configurations
{
    class UserConfiguration : IEntityTypeConfiguration<UserEntity>
    {
        public void Configure(EntityTypeBuilder<UserEntity> modelBuilder)
        {
            modelBuilder.HasIndex(u => u.Email).IsUnique();
            modelBuilder.Property(u => u.Firstname).HasMaxLength(30);
            modelBuilder.Property(u => u.Lastname).HasMaxLength(30);
        }
    }
    class TenderConfiguration : IEntityTypeConfiguration<TenderEntity>
    {
        public void Configure(EntityTypeBuilder<TenderEntity> modelBuilder)
        {
            modelBuilder.HasOne(pt => pt.Calculation).WithMany(p => p.Tenders).HasForeignKey(pt => pt.CalculationId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.HasOne(pt => pt.Organisation).WithMany(p => p.Tenders).HasForeignKey(pt => pt.OrganisationId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.HasMany(pt => pt.TendersAttributes).WithOne(p => p.Tender).HasForeignKey(pt => pt.TenderId).OnDelete(DeleteBehavior.Restrict);
        }
    }
    class TenderAttributeBindConfiguration : IEntityTypeConfiguration<TenderAttributeBindEntity>
    {
        public void Configure(EntityTypeBuilder<TenderAttributeBindEntity> modelBuilder)
        {
            modelBuilder.HasKey(m => new { m.TenderAttributeId, m.TenderId });
            modelBuilder.HasOne(x => x.Tender).WithMany(u => u.TendersAttributes).HasForeignKey(pt => pt.TenderId)
                .HasPrincipalKey(x => x.Id).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.HasOne(pt => pt.TenderAttribute).WithMany(p => p.TendersAttributes)
                .HasForeignKey(pt => pt.TenderAttributeId).OnDelete(DeleteBehavior.Cascade);
        }
    }
    class AttributeNameTenderConfiguration : IEntityTypeConfiguration<AttributeNameTenderEntity>
    {
        public void Configure(EntityTypeBuilder<AttributeNameTenderEntity> modelBuilder)
        {
            modelBuilder.HasOne(pt => pt.Calculation).WithMany(p => p.AttributesTender).HasForeignKey(pt => pt.CalculationId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.HasMany(pt => pt.TendersAttributes).WithOne(p => p.TenderAttribute).HasForeignKey(pt => pt.TenderAttributeId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
