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
    private DotNetObjectReference<ListColumnWidthController>? _ref;
    private Dictionary<string, int> _widths = new();
    private bool _applied;

    /// <summary>Loads persisted widths. Call from the component's initialization.</summary>
    public async Task LoadAsync() => _widths = await loadWidths() ?? new();

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

        _widths[parts[0]] = width;
        await saveWidths(_widths);
    }

    public void Dispose() => _ref?.Dispose();
}
