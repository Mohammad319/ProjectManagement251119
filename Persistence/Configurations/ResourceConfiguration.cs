using Domain.Entities.Calculation;
using Domain.Entities.ResourceType;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Offer;
using ProjectManagement.Shared.DTO.ResourceType;
using System.Text.Json;


namespace Persistence.Configurations
{
    class ResourceTypeConfiguration : IEntityTypeConfiguration<ResourceTypeEntity>
    {
        public void Configure(EntityTypeBuilder<ResourceTypeEntity> modelBuilder)
        {
            modelBuilder.Property(e => e.Metadata).HasConversion(
    v => JsonSerializer.Serialize(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
    v => JsonSerializer.Deserialize<ResourceTypeData>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ResourceTypeData());

            modelBuilder.HasOne(pt => pt.Account).WithMany(p => p.ResourceTypes).HasForeignKey(pt => pt.AccountId).OnDelete(DeleteBehavior.SetNull);
        }
    }
    class ResourceSortConfiguration : IEntityTypeConfiguration<ResourceSortEntity>
    {
        public void Configure(EntityTypeBuilder<ResourceSortEntity> modelBuilder)
        {
            modelBuilder.Property(e => e.Metadata).HasConversion(
    v => JsonSerializer.Serialize(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
    v => JsonSerializer.Deserialize<ResourceTypeData>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ResourceTypeData());

            modelBuilder.HasOne(x => x.ResourceType).WithMany(u => u.ResourcesSort).HasForeignKey(pt => pt.ResourceTypeId)
                .HasPrincipalKey(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.HasOne(pt => pt.Account).WithMany(p => p.ResourceSorts).HasForeignKey(pt => pt.AccountId).OnDelete(DeleteBehavior.SetNull);
        }
    }
    class ResourceConfiguration : IEntityTypeConfiguration<ResourceEntity>
    {
        public void Configure(EntityTypeBuilder<ResourceEntity> modelBuilder)
        {
            modelBuilder.OwnsOne(r => r.Cost, owned =>
                {
                    owned.Property(x => x.BaseCost).HasColumnName("BaseCost");
                    owned.Property(x => x.Cost).HasColumnName("Cost");
                    owned.Property(x => x.ChangeFactor1).HasColumnName("ChangeFactor1");
                    owned.Property(x => x.ChangeFactor2).HasColumnName("ChangeFactor2");
                });
            modelBuilder.Property(e => e.Metadata).HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
                v => JsonSerializer.Deserialize<ResourceData>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ResourceData());

            modelBuilder.HasOne(pt => pt.ResourceSort).WithMany(p => p.Resources).HasForeignKey(pt => pt.ResourceSortId).OnDelete(DeleteBehavior.ClientSetNull);
            modelBuilder.HasOne(pt => pt.ResourceType).WithMany(p => p.Resources).HasForeignKey(pt => pt.ResourceTypeId).OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.HasOne(pt => pt.Account).WithMany(p => p.Resources).HasForeignKey(pt => pt.AccountId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Status).WithMany(p => p.Resources).HasForeignKey(pt => pt.StatusId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Opportunity).WithMany(p => p.Resources).HasForeignKey(pt => pt.OpportunityId).OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
    class TaskConfiguration : IEntityTypeConfiguration<TaskEntity>
    {
        public void Configure(EntityTypeBuilder<TaskEntity> modelBuilder)
        {
            //        modelBuilder.Property(p => p.Metadata)
            //.HasColumnType("jsonb"); // Use "json" for MySQL or "jsonb" for PostgreSQL

            modelBuilder.OwnsOne(t => t.Cost, owned =>
            {
                owned.Property(x => x.BaseCost).HasColumnName("BaseCost");
                owned.Property(x => x.Cost).HasColumnName("Cost");
                owned.Property(x => x.ChangeFactor1).HasColumnName("ChangeFactor1");
                owned.Property(x => x.ChangeFactor2).HasColumnName("ChangeFactor2");
            });

            modelBuilder.Property(e => e.Metadata).HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
                v => JsonSerializer.Deserialize<TaskData>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TaskData());

            //modelBuilder.HasMany(pt => pt.Tasks).WithOne(p => p.ParentTask).HasForeignKey(pt => pt.ParentTaskId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.HasOne(pt => pt.Status).WithMany(p => p.Tasks).HasForeignKey(pt => pt.StatusId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Opportunity).WithMany(p => p.Tasks).HasForeignKey(pt => pt.OpportunityId).OnDelete(DeleteBehavior.ClientSetNull);
        }

    }
    class OfferConfiguration : IEntityTypeConfiguration<OfferEntity>
    {
        public void Configure(EntityTypeBuilder<OfferEntity> modelBuilder)
        {
            modelBuilder.HasOne(o => o.Resource)
       .WithMany(r => r.Offers)
       .HasForeignKey(o => o.ResourceId)
       .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.HasOne(o => o.Organisation)
                   .WithMany()
                   .HasForeignKey(o => o.OrganisationId)
                   .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Property(e => e.Metadata).HasConversion(
    v => JsonSerializer.Serialize(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
    v => JsonSerializer.Deserialize<OfferData>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new OfferData());

            //  modelBuilder.HasOne(pt => pt.Resources).WithMany(p => p.Offers).HasForeignKey(pt => pt.ResourceID).OnDelete(DeleteBehavior.Cascade);

            //No Working ->
            modelBuilder.HasOne(pt => pt.Organisation).WithMany(p => p.Offers).HasForeignKey(pt => pt.OrganisationId).OnDelete(DeleteBehavior.SetNull);
        }
    }
}
