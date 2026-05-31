using Microsoft.JSInterop;

namespace ProjectManagement.Client.Helper;

public static class ReportExportInterop
{
    public static ValueTask ExportExcelAsync(
        IJSRuntime js,
        string fileName,
        string title,
        IEnumerable<string> columns,
        IEnumerable<IEnumerable<object?>> rows) =>
        js.InvokeVoidAsync(
            "pmReportExport.excel",
            fileName,
            title,
            columns.ToArray(),
            NormalizeRows(rows));

    public static ValueTask PrintAsync(
        IJSRuntime js,
        string title,
        IEnumerable<string> columns,
        IEnumerable<IEnumerable<object?>> rows,
        bool pdfMode = false) =>
        js.InvokeVoidAsync(
            "pmReportExport.print",
            title,
            columns.ToArray(),
            NormalizeRows(rows),
            pdfMode);

    private static string[][] NormalizeRows(IEnumerable<IEnumerable<object?>> rows) =>
        rows.Select(row => row.Select(value => value?.ToString() ?? string.Empty).ToArray()).ToArray();
}
