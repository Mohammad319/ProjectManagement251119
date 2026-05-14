using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Feature.PriceImport;

namespace ProjectManagement.Services;

public sealed class PriceImportExtractionClient(
    HttpClient httpClient,
    ILogger<PriceImportExtractionClient> logger) : IPriceImportExtractionClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PriceImportPythonHealthResult?> GetHealthAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<PriceImportPythonHealthResult>("health", JsonOptions, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Price import extraction health check failed.");
            return null;
        }
    }

    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        var health = await GetHealthAsync(ct);
        return health?.Ok == true;
    }

    public async Task<PriceTextExtractionResult?> ExtractTextAsync(
        Stream fileStream,
        string fileName,
        string? contentType = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType);

        form.Add(fileContent, "file", fileName);

        using var response = await httpClient.PostAsync("extract-text", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Price import extraction failed. StatusCode={StatusCode}, FileName={FileName}",
                (int)response.StatusCode,
                fileName);

            return new PriceTextExtractionResult
            {
                Ok = false,
                FileName = fileName,
                Errors = [$"Extraction service returned status {(int)response.StatusCode}."]
            };
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<PriceTextExtractionResult>(JsonOptions, ct);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Price import extraction response could not be parsed. FileName={FileName}", fileName);
            return new PriceTextExtractionResult
            {
                Ok = false,
                FileName = fileName,
                Errors = ["Extraction service returned invalid JSON."]
            };
        }
    }
}
