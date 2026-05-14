using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Feature.PriceImport;
using Microsoft.Extensions.Options;

namespace ProjectManagement.Services;

public sealed class PriceImportAiExtractionService(
    HttpClient httpClient,
    IOptions<PriceImportAiOptions> options,
    ILogger<PriceImportAiExtractionService> logger) : IPriceImportAiExtractionService
{
    private const string SystemPrompt = """
        You extract construction material price items from supplier price documents.

        Return only structured JSON matching the schema.

        Rules:
        - Extract only rows/items that contain a clear product or material name and a price.
        - Do not invent prices.
        - Do not guess missing numbers.
        - If the unit is unclear, set unit to null and lower confidence.
        - If the currency is missing but prices look Swedish, use SEK with lower confidence.
        - If discount is present, extract discountPercent.
        - If both list price and net price are present, fill both.
        - If only one price is present, use basePrice and leave netPrice null unless text clearly says net/netto.
        - Preserve source text.
        - Confidence must be between 0 and 1.
        - If no clear price item exists, return an empty items array.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<AiPriceExtractionResult> ExtractPricesAsync(
        AiPriceExtractionRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            return new AiPriceExtractionResult
            {
                Ok = false,
                Errors = ["AI extraction is disabled. Enable PriceImportAi:Enabled in configuration."]
            };
        }

        if (!string.Equals(settings.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            return new AiPriceExtractionResult
            {
                Ok = false,
                Errors = [$"Unsupported AI provider: {settings.Provider}."]
            };
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return new AiPriceExtractionResult
            {
                Ok = false,
                Errors = ["PriceImportAi API key is missing. Set PriceImportAi__ApiKey as an environment variable."]
            };
        }

        if (request.Blocks.Count == 0)
        {
            return new AiPriceExtractionResult { Ok = true };
        }

        var endpoint = new Uri(new Uri(settings.BaseUrl.TrimEnd('/') + "/"), "chat/completions");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        httpRequest.Content = JsonContent.Create(BuildOpenAiPayload(settings.Model, request), options: JsonOptions);

        logger.LogInformation(
            "Running AI price extraction for ImportJobId={ImportJobId}. Blocks={BlockCount}",
            request.ImportJobId,
            request.Blocks.Count);

        try
        {
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "AI price extraction failed for ImportJobId={ImportJobId}. StatusCode={StatusCode}",
                    request.ImportJobId,
                    (int)response.StatusCode);

                return new AiPriceExtractionResult
                {
                    Ok = false,
                    Errors = [$"AI request failed with status {(int)response.StatusCode}."]
                };
            }

            var content = ExtractAssistantContent(responseText);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new AiPriceExtractionResult
                {
                    Ok = false,
                    Errors = ["AI response did not include JSON content."]
                };
            }

            var result = JsonSerializer.Deserialize<AiPriceExtractionResult>(content, JsonOptions)
                ?? new AiPriceExtractionResult
                {
                    Ok = false,
                    Errors = ["AI response could not be parsed."]
                };

            logger.LogInformation(
                "AI price extraction completed for ImportJobId={ImportJobId}. Items={ItemCount}, Errors={ErrorCount}",
                request.ImportJobId,
                result.Items.Count,
                result.Errors.Count);

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "AI price extraction returned invalid JSON for ImportJobId={ImportJobId}.",
                request.ImportJobId);

            return new AiPriceExtractionResult
            {
                Ok = false,
                Errors = ["AI response could not be parsed."]
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "AI price extraction failed for ImportJobId={ImportJobId}.",
                request.ImportJobId);

            return new AiPriceExtractionResult
            {
                Ok = false,
                Errors = ["AI extraction failed."]
            };
        }
    }

    private static object BuildOpenAiPayload(string model, AiPriceExtractionRequest request)
    {
        return new
        {
            model,
            temperature = 0,
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "price_import_extraction",
                    strict = false,
                    schema = BuildResultSchema()
                }
            },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = SystemPrompt
                },
                new
                {
                    role = "user",
                    content = BuildUserPrompt(request)
                }
            }
        };
    }

    private static string BuildUserPrompt(AiPriceExtractionRequest request)
    {
        var payload = new
        {
            request.ImportJobId,
            request.TenantId,
            request.SupplierName,
            request.SourceFileName,
            request.FileType,
            Blocks = request.Blocks.Select(x => new
            {
                x.BlockId,
                x.PageNumber,
                x.SheetName,
                x.CellRange,
                Text = TruncateBlockText(x.Text)
            }).ToList()
        };

        return "Extract price items from this JSON input. Return JSON only.\n"
            + JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string TruncateBlockText(string text)
        => text.Length <= 1800 ? text : text[..1800];

    private static string? ExtractAssistantContent(string responseText)
    {
        var root = JsonNode.Parse(responseText);
        return root?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
    }

    private static object BuildResultSchema()
    {
        static object NullableString() => new { type = new[] { "string", "null" } };
        static object NullableNumber() => new { type = new[] { "number", "null" } };

        return new
        {
            type = "object",
            additionalProperties = false,
            properties = new
            {
                ok = new { type = "boolean" },
                items = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            articleNumber = NullableString(),
                            productCode = NullableString(),
                            name = new { type = "string" },
                            description = NullableString(),
                            categoryName = NullableString(),
                            basePrice = NullableNumber(),
                            discountPercent = NullableNumber(),
                            netPrice = NullableNumber(),
                            unit = NullableString(),
                            currency = new { type = "string" },
                            supplierName = NullableString(),
                            sourceBlockId = NullableString(),
                            sourceText = NullableString(),
                            confidence = new { type = "number", minimum = 0, maximum = 1 },
                            warning = NullableString()
                        }
                    }
                },
                errors = new
                {
                    type = "array",
                    items = new { type = "string" }
                }
            },
            required = new[] { "ok", "items", "errors" }
        };
    }
}
