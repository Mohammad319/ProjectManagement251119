using Domain.Entities.Base;

namespace Domain.Entities.PriceLists;

public class PriceList : IDataKeyFilterReadOnly
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public string Name { get; set; } = "";
    public string? SupplierName { get; set; }

    public string Currency { get; set; } = "SEK";

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public string? SourceFileName { get; set; }
    public string? SourceFilePath { get; set; }
    public string? SourceFileHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ImportedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Note { get; set; }

    public List<PriceListItem> Items { get; set; } = new();
}
