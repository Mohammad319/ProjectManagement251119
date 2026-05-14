using Domain.Entities.Base;

namespace Domain.Entities.PriceLists;

public class PriceListItem : IDataKeyFilterReadOnly
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public Guid PriceListId { get; set; }
    public PriceList PriceList { get; set; } = default!;

    public string? ArticleNumber { get; set; }
    public string? ProductCode { get; set; }

    public string Name { get; set; } = "";
    public string? Description { get; set; }

    public string? CategoryName { get; set; }
    public string? ClassificationPath { get; set; }

    public decimal? BasePrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? NetPrice { get; set; }

    public string? Unit { get; set; }
    public string Currency { get; set; } = "SEK";

    public string? SupplierName { get; set; }

    public decimal? ConsumptionFactor { get; set; }
    public decimal? WastePercent { get; set; }

    public string? SourceFileName { get; set; }
    public int? SourcePageNumber { get; set; }
    public string? SourceSheetName { get; set; }
    public string? SourceCellRange { get; set; }
    public string? SourceText { get; set; }

    public decimal? Confidence { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }

    public bool IsActive { get; set; } = true;

    public string? UserNote { get; set; }
}
