using Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class CompanyProfileConfiguration : IEntityTypeConfiguration<CompanyProfileEntity>
{
    public void Configure(EntityTypeBuilder<CompanyProfileEntity> builder)
    {
        builder.ToTable("CompanyProfiles");

        // En företagsprofil per tenant — identiteten "ett konto = ett företag".
        builder.HasIndex(x => x.TenantId)
            .IsUnique()
            .HasDatabaseName("IX_CompanyProfiles_Tenant");
    }
}
