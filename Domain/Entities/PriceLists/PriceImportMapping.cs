using Domain.Entities.Base;

namespace Domain.Entities.PriceLists;

public class PriceImportMapping : IDataKeyFilterReadOnly
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public string SupplierName { get; set; } = "";

    public string ExternalColumnName { get; set; } = "";
    public string InternalFieldName { get; set; } = "";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
