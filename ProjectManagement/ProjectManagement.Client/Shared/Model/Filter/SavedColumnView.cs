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

    /// <summary>
    /// Visible-state per column key. Column order is currently fixed by the table
    /// markup, so only visibility is stored; the signature stays order-independent
    /// on purpose. (Order/width can be added later without breaking saved views.)
    /// </summary>
    public Dictionary<string, bool> Columns { get; set; } = new();

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
