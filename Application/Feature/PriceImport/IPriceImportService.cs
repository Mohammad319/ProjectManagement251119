using Domain.Entities.PriceLists;

namespace Application.Feature.PriceImport;

public interface IPriceImportService
{
    Task<IReadOnlyList<PriceImportJobListItemDto>> GetImportJobsAsync(CancellationToken ct = default);
    Task<PriceImportJobDetailsDto?> GetImportJobDetailsAsync(Guid jobId, CancellationToken ct = default);
    Task<IReadOnlyList<PriceListListItemDto>> GetPriceListsAsync(CancellationToken ct = default);
    Task<PriceListDetailsDto?> GetPriceListDetailsAsync(Guid priceListId, CancellationToken ct = default);
    Task<bool> UpdatePriceListAsync(PriceListUpdateDto priceList, CancellationToken ct = default);
    Task<bool> SetPriceListActiveAsync(Guid priceListId, bool isActive, CancellationToken ct = default);
    Task<PriceImportDeleteResultDto> DeletePriceListAsync(Guid priceListId, CancellationToken ct = default);
    Task<bool> UpdatePriceListItemAsync(PriceListItemUpdateDto item, CancellationToken ct = default);
    Task<bool> SetPriceListItemActiveAsync(Guid itemId, bool isActive, CancellationToken ct = default);
    Task<PriceImportDeleteResultDto> DeletePriceListItemAsync(Guid itemId, CancellationToken ct = default);
    Task<Guid> CreateManualTestJobAsync(string? supplierName = null, string? sourceFileName = null, CancellationToken ct = default);
    Task<PriceImportStartResultDto> StartImportFromUploadedFileAsync(
        Stream fileStream,
        string fileName,
        long fileSize,
        string? contentType,
        string? supplierName,
        PriceImportColumnMapping? columnMapping = null,
        CancellationToken ct = default);
    Task<bool> UpdateCandidateStatusAsync(Guid candidateId, PriceImportCandidateStatus status, CancellationToken ct = default);
    Task<bool> UpdateCandidateAsync(PriceImportCandidateUpdateDto candidate, CancellationToken ct = default);
    Task<int> BulkUpdateCandidateStatusAsync(IReadOnlyCollection<Guid> candidateIds, PriceImportCandidateStatus status, CancellationToken ct = default);
    Task<int> ApproveAllReadyCandidatesAsync(Guid jobId, CancellationToken ct = default);
    Task<bool> ApproveCandidateAsync(Guid candidateId, CancellationToken ct = default);
    Task<bool> IgnoreCandidateAsync(Guid candidateId, CancellationToken ct = default);
    Task<Guid?> CreatePriceListFromApprovedCandidatesAsync(Guid jobId, CancellationToken ct = default);
    Task<PriceImportAiRunResultDto> RunAiExtractionForJobAsync(Guid importJobId, CancellationToken ct = default);
    Task<PriceImportDeleteResultDto> DeleteImportJobAsync(Guid jobId, CancellationToken ct = default);
}

public sealed record PriceImportStartResultDto(
    Guid JobId,
    bool Ok,
    string Message);

public sealed record PriceImportDeleteResultDto(
    bool Ok,
    string Message);

public sealed class PriceImportStorageOptions
{
    public string RootPath { get; set; } = "";
    public string StorageRoot { get; set; } = "";
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
    public int MaxUploadSizeMb { get; set; } = 20;
    public string[] AllowedExtensions { get; set; } = [".xlsx", ".xls", ".docx", ".pdf"];
}

public sealed class PriceImportColumnMapping
{
    public int RowStart { get; set; } = 1;
    public int? ArticleCol { get; set; }
    public int? NameCol { get; set; }
    public int? BasePriceCol { get; set; }
    public int? NetPriceCol { get; set; }
    public int? UnitCol { get; set; }
    public int? DiscountCol { get; set; }
    public int? CategoryCol { get; set; }
}

public sealed record PriceImportJobListItemDto(
    Guid Id,
    string SourceFileName,
    string? SupplierName,
    PriceImportFileType FileType,
    PriceImportJobStatus Status,
    int TotalCandidates,
    int ReadyCount,
    int ReviewCount,
    int ErrorCount,
    int ApprovedCount,
    DateTime StartedAt,
    DateTime? CompletedAt);

public sealed record PriceImportJobDetailsDto(
    Guid Id,
    string SourceFileName,
    string SourceFilePath,
    string? SupplierName,
    PriceImportFileType FileType,
    PriceImportJobStatus Status,
    int TotalCandidates,
    int ReadyCount,
    int ReviewCount,
    int ErrorCount,
    int ApprovedCount,
    DateTime StartedAt,
    DateTime? CompletedAt,
    IReadOnlyList<PriceImportCandidateDto> Candidates);

public sealed record PriceListListItemDto(
    Guid Id,
    string Name,
    string? SupplierName,
    string Currency,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    string? SourceFileName,
    DateTime CreatedAt,
    DateTime? ImportedAt,
    bool IsActive,
    int ItemCount);

public sealed record PriceListDetailsDto(
    Guid Id,
    string Name,
    string? SupplierName,
    string Currency,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    string? SourceFileName,
    string? SourceFilePath,
    string? SourceFileHash,
    DateTime CreatedAt,
    DateTime? ImportedAt,
    bool IsActive,
    string? Note,
    IReadOnlyList<PriceListItemDto> Items);

public sealed record PriceListItemDto(
    Guid Id,
    string? ArticleNumber,
    string? ProductCode,
    string Name,
    string? Description,
    string? CategoryName,
    string? ClassificationPath,
    decimal? BasePrice,
    decimal? DiscountPercent,
    decimal? NetPrice,
    string? Unit,
    string Currency,
    string? SupplierName,
    decimal? ConsumptionFactor,
    decimal? WastePercent,
    string? SourceFileName,
    int? SourcePageNumber,
    string? SourceSheetName,
    string? SourceCellRange,
    string? SourceText,
    decimal? Confidence,
    DateTime CreatedAt,
    bool IsActive,
    string? UserNote);

public sealed class PriceListUpdateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? SupplierName { get; set; }
    public string Currency { get; set; } = "SEK";
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
}

public sealed class PriceListItemUpdateDto
{
    public Guid Id { get; set; }
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
    public bool IsActive { get; set; } = true;
    public string? UserNote { get; set; }
}

public sealed record PriceImportCandidateDto(
    Guid Id,
    PriceImportCandidateStatus Status,
    string? ArticleNumber,
    string? ProductCode,
    string Name,
    string? Description,
    string? CategoryName,
    decimal? BasePrice,
    decimal? DiscountPercent,
    decimal? NetPrice,
    string? Unit,
    string Currency,
    string? SupplierName,
    decimal Confidence,
    string? SourceText,
    string? ErrorMessage);

public sealed class PriceImportCandidateUpdateDto
{
    public Guid Id { get; set; }
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
    public string? SourceText { get; set; }
}
