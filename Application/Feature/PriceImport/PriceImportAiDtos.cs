namespace Application.Feature.PriceImport;

public sealed class PriceImportAiOptions
{
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "OpenAI";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
    public string ApiKey { get; set; } = "";
}

public sealed class AiPriceExtractionRequest
{
    public Guid ImportJobId { get; set; }
    public int TenantId { get; set; }
    public string? SupplierName { get; set; }
    public string? SourceFileName { get; set; }
    public string FileType { get; set; } = "";
    public List<AiPriceExtractionBlock> Blocks { get; set; } = new();
}

public sealed class AiPriceExtractionBlock
{
    public string BlockId { get; set; } = "";
    public int? PageNumber { get; set; }
    public string? SheetName { get; set; }
    public string? CellRange { get; set; }
    public string Text { get; set; } = "";
}

public sealed class AiExtractedPriceItem
{
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
    public string? SourceBlockId { get; set; }
    public string? SourceText { get; set; }
    public decimal Confidence { get; set; }
    public string? Warning { get; set; }
}

public sealed class AiPriceExtractionResult
{
    public bool Ok { get; set; }
    public List<AiExtractedPriceItem> Items { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public sealed record PriceImportAiRunResultDto(
    bool Ok,
    int BlocksSent,
    int ItemsExtracted,
    int CandidatesCreated,
    string Message);
