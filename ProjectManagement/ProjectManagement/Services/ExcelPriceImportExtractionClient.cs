using Application.Feature.PriceImport;
using ClosedXML.Excel;

namespace ProjectManagement.Services;

public sealed class ExcelPriceImportExtractionClient : IPriceImportExtractionClient
{
    public Task<PriceImportPythonHealthResult?> GetHealthAsync(CancellationToken ct = default)
        => Task.FromResult<PriceImportPythonHealthResult?>(new() { Ok = true, Service = "excel-csharp" });

    public Task<bool> CheckHealthAsync(CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<PriceTextExtractionResult?> ExtractTextAsync(
        Stream fileStream,
        string fileName,
        string? contentType = null,
        CancellationToken ct = default)
    {
        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var sheets = new List<ExtractedSheet>();

            foreach (var ws in workbook.Worksheets)
            {
                var range = ws.RangeUsed();
                if (range is null) continue;

                var rows = new List<List<string>>();
                foreach (var row in range.RowsUsed())
                {
                    var cells = row.CellsUsed()
                        .Select(c => c.Value.ToString() ?? string.Empty)
                        .ToList();

                    if (cells.Any(c => !string.IsNullOrWhiteSpace(c)))
                        rows.Add(cells);
                }

                if (rows.Count > 0)
                    sheets.Add(new ExtractedSheet { SheetName = ws.Name, Rows = rows });
            }

            return Task.FromResult<PriceTextExtractionResult?>(new()
            {
                Ok = true,
                FileName = fileName,
                FileType = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant(),
                Sheets = sheets
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult<PriceTextExtractionResult?>(new()
            {
                Ok = false,
                FileName = fileName,
                Errors = [ex.Message]
            });
        }
    }
}
