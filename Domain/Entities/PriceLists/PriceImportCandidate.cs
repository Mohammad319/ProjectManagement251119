using Domain.Entities.Base;

namespace Domain.Entities.PriceLists;

public class PriceImportCandidate : IDataKeyFilterReadOnly
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public Guid ImportJobId { get; set; }
    public PriceImportJob ImportJob { get; set; } = default!;

    public string? ArticleNumber { get; set; }
    public string? ProductCode { get; set; }

    public string Name { get; set; } = "";

    public string? Description { get; set; }
    public string? CategoryName { get; set; }

    public decimal? BasePrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? NetPrice { get; set; }

    public string? Unit { get; set; }
    public string Currency { get; set; } = "SEK";

    public string? SupplierName { get; set; }

    public int? PageNumber { get; set; }
    public string? SheetName { get; set; }
    public string? CellRange { get; set; }

    public string? SourceText { get; set; }
    public string? RawJson { get; set; }

    public decimal Confidence { get; set; }

    public PriceImportCandidateStatus Status { get; set; } = PriceImportCandidateStatus.NeedsReview;

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
