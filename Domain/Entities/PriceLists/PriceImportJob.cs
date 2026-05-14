using Domain.Entities.Base;

namespace Domain.Entities.PriceLists;

public class PriceImportJob : IDataKeyFilterReadOnly
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public string? SupplierName { get; set; }

    public string SourceFileName { get; set; } = "";
    public string SourceFilePath { get; set; } = "";
    public string? SourceFileHash { get; set; }

    public PriceImportFileType FileType { get; set; } = PriceImportFileType.Unknown;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public PriceImportJobStatus Status { get; set; } = PriceImportJobStatus.Pending;

    public int TotalCandidates { get; set; }
    public int ReadyCount { get; set; }
    public int ReviewCount { get; set; }
    public int ErrorCount { get; set; }
    public int ApprovedCount { get; set; }

    public string? ErrorMessage { get; set; }

    public List<PriceImportCandidate> Candidates { get; set; } = new();
}
