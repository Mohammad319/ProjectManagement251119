namespace Domain.Entities.PriceLists;

public enum PriceImportJobStatus
{
    Pending = 1,
    Analyzing = 2,
    ReadyForReview = 3,
    Completed = 4,
    Failed = 5
}

public enum PriceImportCandidateStatus
{
    Ready = 1,
    NeedsReview = 2,
    Approved = 3,
    Ignored = 4,
    Error = 5
}

public enum PriceImportFileType
{
    Excel = 1,
    Word = 2,
    PdfText = 3,
    PdfScan = 4,
    Unknown = 99
}
