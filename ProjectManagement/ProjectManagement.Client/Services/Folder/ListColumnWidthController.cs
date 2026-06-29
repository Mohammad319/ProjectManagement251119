using Microsoft.JSInterop;

namespace ProjectManagement.Client.Services.Folder;

/// <summary>
/// Shared wiring for a right-panel list's resizable columns. Encapsulates the
/// boilerplate the project and calculation lists previously each carried: holding
/// the per-column widths, owning the <see cref="DotNetObjectReference{T}"/> handed
/// to the JS engine, and persisting a width when the user drags a column edge.
///
/// The resizable/frozen-column engine itself lives in BlazorMHD.UI (its JS module
/// exports <c>tableColumns</c>); the app's <c>resizableTable.js</c> adapter exposes
/// the <c>initializeResizableColumns</c> / <c>applySavedColumnWidths</c> globals and
/// the PM table conventions (<c>#resizeMe</c>, frozen-column attrs). This controller
/// only drives those globals and routes the save callback to a per-list delegate
/// (each list has its own typed <c>…ViewPreference</c>), so the persisted shape is
/// unchanged.
/// </summary>
public sealed class ListColumnWidthController(
    IJSRuntime js,
    Func<Task<Dictionary<string, int>?>> loadWidths,
    Func<Dictionary<string, int>, Task> saveWidths) : IDisposable
{
    private static readonly IReadOnlyDictionary<string, (int Min, int Max)> ColumnBounds = new Dictionary<string, (int Min, int Max)>(StringComparer.OrdinalIgnoreCase)
    {
        ["rowNumber"] = (45, 55),
        ["code"] = (55, 160),
        ["name"] = (150, 520),
        ["project"] = (150, 520),
        ["status"] = (100, 260),
        ["calculationType"] = (100, 260),
        ["projectType"] = (100, 260),
        ["access"] = (120, 260),
        ["shared"] = (120, 260),
        ["archived"] = (80, 220),
        ["deadline"] = (110, 180),
        ["createdAt"] = (110, 180),
        ["updatedAt"] = (110, 180),
        ["publicationDate"] = (110, 180),
        ["decisionDate"] = (110, 180),
        ["start"] = (110, 180),
        ["end"] = (110, 180),
        ["qa"] = (110, 180),
        ["department"] = (90, 260),
        ["folder"] = (90, 260),
        ["responsible"] = (100, 300),
        ["inspector"] = (100, 300),
        ["organisation"] = (120, 360),
        ["procurementNumber"] = (120, 360),
        ["customerReference"] = (120, 360),
        ["procurementName"] = (120, 360),
        ["procurementMethods"] = (110, 360),
        ["contract"] = (90, 260),
        ["compensation"] = (110, 260),
        ["procurementProcedure"] = (130, 360),
        ["byggherre"] = (120, 360),
        ["clientsManager"] = (120, 360),
        ["designer"] = (120, 360),
        ["address"] = (140, 360),
        ["supervisor"] = (120, 360),
        ["version"] = (70, 160),
        ["calculationRole"] = (130, 260),
        ["priority"] = (80, 220),
        ["timeMonth"] = (80, 220),
        ["tax"] = (60, 140),
        ["privacy"] = (60, 140),
    };

    private DotNetObjectReference<ListColumnWidthController>? _ref;
    private Dictionary<string, int> _widths = new();
    private bool _applied;

    /// <summary>Loads persisted widths. Call from the component's initialization.</summary>
    public async Task LoadAsync()
    {
        var loaded = await loadWidths() ?? new();
        _widths = loaded.ToDictionary(
            pair => pair.Key,
            pair => ClampColumnWidth(pair.Key, pair.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes the JS resize engine against the current <c>#resizeMe</c> table and
    /// applies the saved widths once. Safe to call on every render; the widths are
    /// applied only the first time there is something to apply. Call from
    /// <c>OnAfterRenderAsync</c> (skip when this component does not own the table).
    /// </summary>
    public async Task AttachAsync()
    {
        _ref ??= DotNetObjectReference.Create(this);
        await js.InvokeVoidAsync("initializeResizableColumns", _ref);

        if (!_applied && _widths.Count > 0)
        {
            _applied = true;
            await js.InvokeVoidAsync("applySavedColumnWidths", _widths);
        }
    }

    /// <summary>
    /// Called from JS when a column is resized. Payload is "columnKey||width"
    /// (e.g. "code||180"). Persists the new width via the supplied delegate.
    /// </summary>
    [JSInvokable]
    public async Task SaveTemplateBlazor(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return;

        var parts = data.Split("||", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !int.TryParse(parts[1], out var width) || width <= 0)
            return;

        var columnKey = parts[0];
        var clampedWidth = ClampColumnWidth(columnKey, width);

        _widths[columnKey] = clampedWidth;
        await saveWidths(_widths);

        if (clampedWidth != width)
            await js.InvokeVoidAsync("applySavedColumnWidths", new Dictionary<string, int> { [columnKey] = clampedWidth });
    }

    private static int ClampColumnWidth(string columnKey, int width)
    {
        if (!ColumnBounds.TryGetValue(columnKey, out var bounds))
            bounds = (80, 360);

        return Math.Clamp(width, bounds.Min, bounds.Max);
    }

    public void Dispose() => _ref?.Dispose();
}
