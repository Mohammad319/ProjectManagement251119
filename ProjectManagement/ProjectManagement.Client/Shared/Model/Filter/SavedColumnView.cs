namespace ProjectManagement.Client.Shared.Model.Filter;

/// <summary>
/// A named, personal column layout for the right-panel lists ("kolumnvy").
/// A column view describes ONLY how the list is shown — which columns are
/// visible (and, where supported, their order). It never contains filters,
/// search text, status/archived selection or "show all versions": those
/// belong to a <see cref="SavedListFilter"/>.
///
/// Project list and calculation list views are stored under separate scopes
/// and must never be mixed (they have different columns).
/// </summary>
public sealed class SavedColumnView
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Visible-state per column key.</summary>
    public Dictionary<string, bool> Columns { get; set; } = new();

    /// <summary>
    /// Column key order (left → right) for this view, including hidden columns so a
    /// re-enabled extra column lands in its saved position. Empty for legacy views
    /// saved before ordering existed (callers fall back to the table's natural order).
    /// </summary>
    public List<string> Order { get; set; } = new();

    /// <summary>
    /// Normalized, order-independent signature of the visible column set. Two views
    /// that show exactly the same columns produce the same signature regardless of
    /// name. Used to detect duplicate views and to recognise when the current layout
    /// already matches a saved view.
    /// </summary>
    public string ContentSignature() => BuildSignature(Columns);

    public static string BuildSignature(IReadOnlyDictionary<string, bool> columns) =>
        string.Join(",", columns
            .Where(kv => kv.Value)
            .Select(kv => kv.Key)
            .OrderBy(key => key, StringComparer.Ordinal));
}
