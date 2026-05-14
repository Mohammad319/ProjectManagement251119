using System.Text.Json.Serialization;

namespace Application.Feature.PriceImport;

public sealed class PriceTextExtractionResult
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = "";

    [JsonPropertyName("fileType")]
    public string FileType { get; set; } = "";

    [JsonPropertyName("pages")]
    public List<ExtractedTextPage> Pages { get; set; } = [];

    [JsonPropertyName("sheets")]
    public List<ExtractedSheet> Sheets { get; set; } = [];

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = [];
}

public sealed class ExtractedTextPage
{
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

public sealed class ExtractedSheet
{
    [JsonPropertyName("sheetName")]
    public string SheetName { get; set; } = "";

    [JsonPropertyName("rows")]
    public List<List<string>> Rows { get; set; } = [];
}

public sealed class PriceImportPythonHealthResult
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("service")]
    public string Service { get; set; } = "";
}
