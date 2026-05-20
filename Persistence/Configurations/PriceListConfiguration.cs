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

        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_PriceLists_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_PriceLists_DateRange", "[ValidTo] IS NULL OR [ValidFrom] IS NULL OR [ValidTo] >= [ValidFrom]");
        });
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

        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_PriceListItems_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_PriceListItems_BasePrice_NonNegative", "[BasePrice] IS NULL OR [BasePrice] >= 0");
            t.HasCheckConstraint("CK_PriceListItems_NetPrice_NonNegative", "[NetPrice] IS NULL OR [NetPrice] >= 0");
            t.HasCheckConstraint("CK_PriceListItems_DiscountPercent_Range", "[DiscountPercent] IS NULL OR ([DiscountPercent] >= 0 AND [DiscountPercent] <= 100)");
            t.HasCheckConstraint("CK_PriceListItems_ConsumptionFactor_NonNegative", "[ConsumptionFactor] IS NULL OR [ConsumptionFactor] >= 0");
            t.HasCheckConstraint("CK_PriceListItems_WastePercent_Range", "[WastePercent] IS NULL OR ([WastePercent] >= 0 AND [WastePercent] <= 100)");
            t.HasCheckConstraint("CK_PriceListItems_Confidence_Range", "[Confidence] IS NULL OR ([Confidence] >= 0 AND [Confidence] <= 1)");
        });
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

        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_PriceImportJobs_SourceFileName_NotEmpty", "LEN(LTRIM(RTRIM([SourceFileName]))) > 0");
            t.HasCheckConstraint("CK_PriceImportJobs_SourceFilePath_NotEmpty", "LEN(LTRIM(RTRIM([SourceFilePath]))) > 0");
            t.HasCheckConstraint("CK_PriceImportJobs_Counts_NonNegative", "[TotalCandidates] >= 0 AND [ReadyCount] >= 0 AND [ReviewCount] >= 0 AND [ErrorCount] >= 0 AND [ApprovedCount] >= 0");
            t.HasCheckConstraint("CK_PriceImportJobs_DateRange", "[CompletedAt] IS NULL OR [CompletedAt] >= [StartedAt]");
        });
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

        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_PriceImportCandidates_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_PriceImportCandidates_BasePrice_NonNegative", "[BasePrice] IS NULL OR [BasePrice] >= 0");
            t.HasCheckConstraint("CK_PriceImportCandidates_NetPrice_NonNegative", "[NetPrice] IS NULL OR [NetPrice] >= 0");
            t.HasCheckConstraint("CK_PriceImportCandidates_DiscountPercent_Range", "[DiscountPercent] IS NULL OR ([DiscountPercent] >= 0 AND [DiscountPercent] <= 100)");
            t.HasCheckConstraint("CK_PriceImportCandidates_Confidence_Range", "[Confidence] >= 0 AND [Confidence] <= 1");
        });
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

        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_PriceImportMappings_SupplierName_NotEmpty", "LEN(LTRIM(RTRIM([SupplierName]))) > 0");
            t.HasCheckConstraint("CK_PriceImportMappings_ExternalColumnName_NotEmpty", "LEN(LTRIM(RTRIM([ExternalColumnName]))) > 0");
            t.HasCheckConstraint("CK_PriceImportMappings_InternalFieldName_NotEmpty", "LEN(LTRIM(RTRIM([InternalFieldName]))) > 0");
        });
    }
}
