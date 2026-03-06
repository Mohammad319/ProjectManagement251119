using Domain.Entities.Organisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class OrganisationCategoryConfiguration : IEntityTypeConfiguration<OrganisationCategoryEntity>
{
    public void Configure(EntityTypeBuilder<OrganisationCategoryEntity> builder)
    {
        builder.ToTable("OrganisationCategories");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("UX_OrganisationCategories_Tenant_Name");

        builder.HasIndex(x => new { x.TenantId, x.ParentCategoryId })
            .HasDatabaseName("IX_OrganisationCategories_Tenant_Parent");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_OrganisationCategories_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_OrganisationCategories_Parent_Positive", "[ParentCategoryId] IS NULL OR [ParentCategoryId] > 0");
        });

        builder.HasOne(x => x.ParentCategory)
            .WithMany(x => x.ChildCategories)
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OrganisationTypeConfiguration : IEntityTypeConfiguration<OrganisationTypeEntity>
{
    public void Configure(EntityTypeBuilder<OrganisationTypeEntity> builder)
    {
        builder.ToTable("OrganisationTypes");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("UX_OrganisationTypes_Tenant_Name");

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_OrganisationTypes_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0")
        );
    }
}

internal sealed class OrganisationConfiguration : IEntityTypeConfiguration<OrganisationEntity>
{
    public void Configure(EntityTypeBuilder<OrganisationEntity> builder)
    {
        builder.ToTable("Organisations");

        builder.HasIndex(x => new { x.TenantId, x.IsVisible, x.Name })
            .HasDatabaseName("IX_Organisations_Tenant_Visible_Name");

        builder.HasIndex(x => new { x.TenantId, x.OrganisationCategoryId })
            .HasDatabaseName("IX_Organisations_Tenant_Category");

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Organisations_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0")
        );

        builder.HasOne(x => x.OrganisationCategory)
            .WithMany(x => x.Organisations)
            .HasForeignKey(x => x.OrganisationCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OrganisationType)
            .WithMany(x => x.Organisations)
            .HasForeignKey(x => x.OrganisationTypeId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
