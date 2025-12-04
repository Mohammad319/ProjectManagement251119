using Domain.Entities.Calculation;
using Domain.Entities.ResourceType;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json;
using ProjectManagement.Shared.DTO.Calculation;
using System.Text.Json.Serialization.Metadata;
using System.Collections.Generic;
using ProjectManagement.Shared.DTO.ResourceType;
using ProjectManagement.Shared.DTO.Offer;
using static ProjectManagement.Shared.Constant.URLConst;
using ProjectManagement.Shared.Base.Calculation;


namespace Persistence.Configurations
{
    class ResourceTypeConfiguration : IEntityTypeConfiguration<ResourceTypeEntity>
    {
        public void Configure(EntityTypeBuilder<ResourceTypeEntity> modelBuilder)
        {
            modelBuilder.Property(e => e.Data).HasConversion(
    v => JsonSerializer.Serialize(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
    v => JsonSerializer.Deserialize<ResourceTypeData>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ResourceTypeData());

            modelBuilder.HasOne(pt => pt.Account).WithMany(p => p.ResourcesType).HasForeignKey(pt => pt.AccountId).OnDelete(DeleteBehavior.SetNull);
        }
    }
    class ResourceSortConfiguration : IEntityTypeConfiguration<ResourceSortEntity>
    {
        public void Configure(EntityTypeBuilder<ResourceSortEntity> modelBuilder)
        {
            modelBuilder.Property(e => e.Data).HasConversion(
    v => JsonSerializer.Serialize(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
    v => JsonSerializer.Deserialize<ResourceTypeData>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ResourceTypeData());

            modelBuilder.HasOne(x => x.ResourceType).WithMany(u => u.ResourcesSort).HasForeignKey(pt => pt.ResourceTypeId)
                .HasPrincipalKey(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.HasOne(pt => pt.Account).WithMany(p => p.ResourcesSort).HasForeignKey(pt => pt.AccountId).OnDelete(DeleteBehavior.SetNull);
        }
    }
    class ResourceConfiguration : IEntityTypeConfiguration<ResourceEntity>
    {
        public void Configure(EntityTypeBuilder<ResourceEntity> modelBuilder)
        {
            modelBuilder.Property(e => e.Data).HasConversion(
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
    //        modelBuilder.Property(p => p.Data)
    //.HasColumnType("jsonb"); // Use "json" for MySQL or "jsonb" for PostgreSQL

            modelBuilder.Property(e => e.Data).HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
                v => JsonSerializer.Deserialize<TaskData>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TaskData());

            //modelBuilder.HasMany(pt => pt.Tasks).WithOne(p => p.Task).HasForeignKey(pt => pt.TaskId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.HasOne(pt => pt.Status).WithMany(p => p.Tasks).HasForeignKey(pt => pt.StatusId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Opportunity).WithMany(p => p.Tasks).HasForeignKey(pt => pt.OpportunityId).OnDelete(DeleteBehavior.ClientSetNull);
        }

    }
    class OfferConfiguration : IEntityTypeConfiguration<OfferEntity>
    {
        public void Configure(EntityTypeBuilder<OfferEntity> modelBuilder)
        {
            modelBuilder.Property(e => e.Data).HasConversion(
    v => JsonSerializer.Serialize(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
    v => JsonSerializer.Deserialize<OfferData>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new OfferData());

          //  modelBuilder.HasOne(pt => pt.Resource).WithMany(p => p.Offers).HasForeignKey(pt => pt.ResourceID).OnDelete(DeleteBehavior.Cascade);

            //No Working ->
            modelBuilder.HasOne(pt => pt.Organisation).WithMany(p => p.Offers).HasForeignKey(pt => pt.OrganisationId).OnDelete(DeleteBehavior.SetNull);
        }
    }
}
