namespace Application.Feature.PriceImport;

public interface IPriceImportAiExtractionService
{
    Task<AiPriceExtractionResult> ExtractPricesAsync(
        AiPriceExtractionRequest request,
        CancellationToken cancellationToken = default);
}
