namespace Application.Feature.PriceImport;

public interface IPriceImportExtractionClient
{
    Task<PriceImportPythonHealthResult?> GetHealthAsync(CancellationToken ct = default);
    Task<bool> CheckHealthAsync(CancellationToken ct = default);

    Task<PriceTextExtractionResult?> ExtractTextAsync(
        Stream fileStream,
        string fileName,
        string? contentType = null,
        CancellationToken ct = default);
}
