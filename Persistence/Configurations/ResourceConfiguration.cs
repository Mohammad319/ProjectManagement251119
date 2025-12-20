using Domain.Entities.Calculation;
using Domain.Entities.ResourceType;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Serialization;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Offer;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Persistence.Configurations;

internal sealed class ResourceTypeConfiguration : IEntityTypeConfiguration<ResourceTypeEntity>
{
    public void Configure(EntityTypeBuilder<ResourceTypeEntity> builder)
    {
        builder.Property(e => e.Metadata)
            .HasJsonConversion<ResourceTypeData>();

        builder.HasOne(x => x.Account)
            .WithMany(x => x.ResourceTypes)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class ResourceSortConfiguration : IEntityTypeConfiguration<ResourceSortEntity>
{
    public void Configure(EntityTypeBuilder<ResourceSortEntity> builder)
    {
        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        builder.HasOne(x => x.ResourceType)
            .WithMany(x => x.ResourcesSort)
            .HasForeignKey(x => x.ResourceTypeId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Account)
            .WithMany(x => x.ResourceSorts)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class ResourceConfiguration : IEntityTypeConfiguration<ResourceEntity>
{
    public void Configure(EntityTypeBuilder<ResourceEntity> builder)
    {
        builder.OwnsOne(r => r.Cost, owned =>
        {
            owned.Property(x => x.BaseCost).HasColumnName("BaseCost");
            owned.Property(x => x.Cost).HasColumnName("Cost");
            owned.Property(x => x.ChangeFactor1).HasColumnName("ChangeFactor1");
            owned.Property(x => x.ChangeFactor2).HasColumnName("ChangeFactor2");
        });

        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        builder.HasOne(x => x.ResourceSort)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.ResourceSortId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.ResourceType)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.ResourceTypeId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Account)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Status)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Opportunity)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.OpportunityId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}

internal sealed class TaskConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.OwnsOne(t => t.Cost, owned =>
        {
            owned.Property(x => x.BaseCost).HasColumnName("BaseCost");
            owned.Property(x => x.Cost).HasColumnName("Cost");
            owned.Property(x => x.ChangeFactor1).HasColumnName("ChangeFactor1");
            owned.Property(x => x.ChangeFactor2).HasColumnName("ChangeFactor2");
        });

        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        builder.HasOne(x => x.Status)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Opportunity)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.OpportunityId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}

internal sealed class OfferConfiguration : IEntityTypeConfiguration<OfferEntity>
{
    public void Configure(EntityTypeBuilder<OfferEntity> builder)
    {
        builder.HasOne(o => o.Resource)
            .WithMany(r => r.Offers)
            .HasForeignKey(o => o.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        // كان عندك تعريفان لعلاقة Organisation (واحد Restrict وواحد SetNull)
        // نخليها واحدة واضحة:
        builder.HasOne(o => o.Organisation)
            .WithMany(p => p.Offers)
            .HasForeignKey(o => o.OrganisationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.Metadata)
            .HasJsonConversion();
    }
}
