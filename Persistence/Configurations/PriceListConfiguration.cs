using Domain.Entities.PriceLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal sealed class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.EntityPriceList();
    }
}

internal sealed class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> builder)
    {
        builder.EntityPriceListItem();
    }
}

internal sealed class PriceImportJobConfiguration : IEntityTypeConfiguration<PriceImportJob>
{
    public void Configure(EntityTypeBuilder<PriceImportJob> builder)
    {
        builder.EntityPriceImportJob();
    }
}

internal sealed class PriceImportCandidateConfiguration : IEntityTypeConfiguration<PriceImportCandidate>
{
    public void Configure(EntityTypeBuilder<PriceImportCandidate> builder)
    {
        builder.EntityPriceImportCandidate();
    }
}

internal sealed class PriceImportMappingConfiguration : IEntityTypeConfiguration<PriceImportMapping>
{
    public void Configure(EntityTypeBuilder<PriceImportMapping> builder)
    {
        builder.EntityPriceImportMapping();
    }
}

internal static class PriceListConfigurationExtensions
{
    internal static void EntityPriceList(this EntityTypeBuilder<PriceList> entity)
    {
        entity.HasKey(x => x.Id);

        entity.HasMany(x => x.Items)
            .WithOne(x => x.PriceList)
            .HasForeignKey(x => x.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.TenantId, x.IsActive });
        entity.HasIndex(x => new { x.TenantId, x.SupplierName });
        entity.HasIndex(x => new { x.TenantId, x.SourceFileHash });

        entity.Property(x => x.Name)
            .HasMaxLength(300);

        entity.Property(x => x.SupplierName)
            .HasMaxLength(300);

        entity.Property(x => x.Currency)
            .HasMaxLength(10);
    }

    internal static void EntityPriceListItem(this EntityTypeBuilder<PriceListItem> entity)
    {
        entity.HasKey(x => x.Id);

        entity.HasIndex(x => new { x.TenantId, x.PriceListId });
        entity.HasIndex(x => new { x.TenantId, x.PriceListId, x.ArticleNumber });
        entity.HasIndex(x => new { x.TenantId, x.IsActive });
        entity.HasIndex(x => new { x.TenantId, x.Name });
        entity.HasIndex(x => new { x.TenantId, x.ProductCode });
        entity.HasIndex(x => new { x.TenantId, x.SupplierName });

        entity.Property(x => x.Name)
            .HasMaxLength(500);

        entity.Property(x => x.ArticleNumber)
            .HasMaxLength(150);

        entity.Property(x => x.ProductCode)
            .HasMaxLength(150);

        entity.Property(x => x.Unit)
            .HasMaxLength(50);

        entity.Property(x => x.Currency)
            .HasMaxLength(10);

        entity.Property(x => x.BasePrice)
            .HasPrecision(18, 4);

        entity.Property(x => x.NetPrice)
            .HasPrecision(18, 4);

        entity.Property(x => x.DiscountPercent)
            .HasPrecision(9, 4);

        entity.Property(x => x.ConsumptionFactor)
            .HasPrecision(18, 6);

        entity.Property(x => x.WastePercent)
            .HasPrecision(9, 4);

        entity.Property(x => x.Confidence)
            .HasPrecision(5, 4);
    }

    internal static void EntityPriceImportJob(this EntityTypeBuilder<PriceImportJob> entity)
    {
        entity.HasKey(x => x.Id);

        entity.HasMany(x => x.Candidates)
            .WithOne(x => x.ImportJob)
            .HasForeignKey(x => x.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.TenantId, x.Status });
        entity.HasIndex(x => new { x.TenantId, x.SupplierName });
        entity.HasIndex(x => new { x.TenantId, x.SourceFileHash });

        entity.Property(x => x.SourceFileName)
            .HasMaxLength(500);

        entity.Property(x => x.SourceFilePath)
            .HasMaxLength(1000);

        entity.Property(x => x.SupplierName)
            .HasMaxLength(300);
    }

    internal static void EntityPriceImportCandidate(this EntityTypeBuilder<PriceImportCandidate> entity)
    {
        entity.HasKey(x => x.Id);

        entity.HasIndex(x => new { x.TenantId, x.ImportJobId, x.Status });
        entity.HasIndex(x => new { x.TenantId, x.Status });
        entity.HasIndex(x => new { x.TenantId, x.Name });
        entity.HasIndex(x => new { x.TenantId, x.ArticleNumber });
        entity.HasIndex(x => new { x.TenantId, x.ProductCode });

        entity.Property(x => x.Name)
            .HasMaxLength(500);

        entity.Property(x => x.ArticleNumber)
            .HasMaxLength(150);

        entity.Property(x => x.ProductCode)
            .HasMaxLength(150);

        entity.Property(x => x.Unit)
            .HasMaxLength(50);

        entity.Property(x => x.Currency)
            .HasMaxLength(10);

        entity.Property(x => x.BasePrice)
            .HasPrecision(18, 4);

        entity.Property(x => x.NetPrice)
            .HasPrecision(18, 4);

        entity.Property(x => x.DiscountPercent)
            .HasPrecision(9, 4);

        entity.Property(x => x.Confidence)
            .HasPrecision(5, 4);
    }

    internal static void EntityPriceImportMapping(this EntityTypeBuilder<PriceImportMapping> entity)
    {
        entity.HasKey(x => x.Id);

        entity.HasIndex(x => new
        {
            x.TenantId,
            x.SupplierName,
            x.ExternalColumnName
        });

        entity.Property(x => x.SupplierName)
            .HasMaxLength(300);

        entity.Property(x => x.ExternalColumnName)
            .HasMaxLength(300);

        entity.Property(x => x.InternalFieldName)
            .HasMaxLength(150);
    }
}
