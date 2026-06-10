using Microsoft.JSInterop;

namespace ProjectManagement.Client.Helper;

public sealed record ReportExportSection(string Heading, IReadOnlyList<string?> Lines);

public sealed record ReportExportKpi(string Label, string Value);

public sealed record ReportExportHeader(
    string Title,
    IReadOnlyList<ReportExportSection>? Sections = null,
    IReadOnlyList<ReportExportKpi>? Kpis = null)
{
    public string CreatedDate { get; init; } = DateTime.Now.ToString("yyyy-MM-dd");
}

public static class ReportExportInterop
{
    public static ValueTask ExportExcelAsync(
        IJSRuntime js,
        string fileName,
        ReportExportHeader header,
        IEnumerable<string> columns,
        IEnumerable<IEnumerable<object?>> rows) =>
        js.InvokeVoidAsync(
            "pmReportExport.excel",
            fileName,
            ToJsHeader(header),
            columns.ToArray(),
            NormalizeRows(rows));

    public static ValueTask ExportExcelAsync(
        IJSRuntime js,
        string fileName,
        string title,
        IEnumerable<string> columns,
        IEnumerable<IEnumerable<object?>> rows) =>
        ExportExcelAsync(js, fileName, new ReportExportHeader(title), columns, rows);

    public static ValueTask PrintAsync(
        IJSRuntime js,
        ReportExportHeader header,
        IEnumerable<string> columns,
        IEnumerable<IEnumerable<object?>> rows,
        bool pdfMode = false) =>
        js.InvokeVoidAsync(
            "pmReportExport.print",
            ToJsHeader(header),
            columns.ToArray(),
            NormalizeRows(rows),
            pdfMode);

    public static ValueTask PrintAsync(
        IJSRuntime js,
        string title,
        IEnumerable<string> columns,
        IEnumerable<IEnumerable<object?>> rows,
        bool pdfMode = false) =>
        PrintAsync(js, new ReportExportHeader(title), columns, rows, pdfMode);

    private static object ToJsHeader(ReportExportHeader header) => new
    {
        title = header.Title,
        date = header.CreatedDate,
        sections = (header.Sections ?? [])
            .Select(s => new
            {
                heading = s.Heading,
                lines = s.Lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l!).ToArray()
            })
            .Where(s => s.lines.Length > 0)
            .ToArray(),
        kpis = (header.Kpis ?? []).Select(k => new { label = k.Label, value = k.Value }).ToArray()
    };

    private static string[][] NormalizeRows(IEnumerable<IEnumerable<object?>> rows) =>
        rows.Select(row => row.Select(value => value?.ToString() ?? string.Empty).ToArray()).ToArray();
}
