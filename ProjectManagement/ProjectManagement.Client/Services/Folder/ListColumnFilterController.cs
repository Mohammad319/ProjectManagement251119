using ProjectManagement.Client.Shared.Model.Filter;

namespace ProjectManagement.Client.Services.Folder;

/// <summary>
/// Shared per-column filter management for the right-panel list tables (project list
/// and calculation list). Both previously carried byte-identical logic for: active/clear
/// helpers, the "hidden filter column" floating notice, and the personal saved-filters
/// (build/match/save/apply/delete + persistence).
///
/// The host keeps owning the live <c>Dictionary&lt;string, ColumnFilterState&gt;</c>
/// (so its domain-specific filter <c>switch</c> and dropdown-value population are
/// untouched); this controller layers the identical management logic on top, reading
/// that same dictionary by reference. Sort state is read from the shared
/// <see cref="ListSortController"/> so saved filters round-trip the sort too.
/// </summary>
public sealed class ListColumnFilterController
{
    private readonly IReadOnlyDictionary<string, ColumnFilterState> _filters;
    private readonly ListSavedFilterStorage _savedStorage;
    private readonly string _scope;
    private readonly ListSortController _sort;
    private readonly Func<string, bool> _columnExists;
    private readonly Func<string, bool> _isColumnVisible;
    private readonly Func<string, string> _columnLabel;
    private readonly Action<string> _revealColumn;
    private readonly Func<Task> _persistColumns;
    private readonly Action _onChanged;
    // Calculation list only — the saved filter also captures the "show all versions"
    // toggle because it affects which rows the filter selects. Null on the project list.
    private readonly Func<bool?>? _getShowAllVersions;
    private readonly Action<bool>? _setShowAllVersions;

    public ListColumnFilterController(
        IReadOnlyDictionary<string, ColumnFilterState> filters,
        ListSavedFilterStorage savedStorage,
        string scope,
        ListSortController sort,
        Func<string, bool> columnExists,
        Func<string, bool> isColumnVisible,
        Func<string, string> columnLabel,
        Action<string> revealColumn,
        Func<Task> persistColumns,
        Action onChanged,
        Func<bool?>? getShowAllVersions = null,
        Action<bool>? setShowAllVersions = null)
    {
        _filters = filters;
        _savedStorage = savedStorage;
        _scope = scope;
        _sort = sort;
        _columnExists = columnExists;
        _isColumnVisible = isColumnVisible;
        _columnLabel = columnLabel;
        _revealColumn = revealColumn;
        _persistColumns = persistColumns;
        _onChanged = onChanged;
        _getShowAllVersions = getShowAllVersions;
        _setShowAllVersions = setShowAllVersions;
    }

    // ── Active / clear ──────────────────────────────────────────────────
    public bool HasActive => _filters.Values.Any(f => f.IsActive);
    public int ActiveCount => _filters.Values.Count(f => f.IsActive);

    public void Clear()
    {
        foreach (var f in _filters.Values) f.Clear();
        _onChanged();
    }

    public void ClearSingle(string key)
    {
        _filters[key].Clear();
        _onChanged();
    }

    // ── Hidden filter columns ───────────────────────────────────────────
    // Filter keys map 1:1 to column keys, so an active filter on a hidden column still
    // works but is invisible. We surface that and offer to reveal those columns WITHOUT
    // touching the filter itself. The floating notice re-appears whenever the set of
    // hidden filter-columns changes (different from the last dismissed signature).
    private string? _dismissedHiddenSignature;

    public IEnumerable<string> HiddenActiveColumnKeys =>
        _filters
            .Where(kv => kv.Value.IsActive && _columnExists(kv.Key) && !_isColumnVisible(kv.Key))
            .Select(kv => kv.Key);

    public bool HasHiddenActiveColumns => HiddenActiveColumnKeys.Any();

    public string HiddenColumnsLabel =>
        string.Join(", ", HiddenActiveColumnKeys.Select(_columnLabel));

    private string HiddenSignature =>
        string.Join("|", HiddenActiveColumnKeys.OrderBy(k => k, StringComparer.Ordinal));

    public bool ShouldShowNotice(bool filterMenuOpen) =>
        HasHiddenActiveColumns && !filterMenuOpen && _dismissedHiddenSignature != HiddenSignature;

    public string HiddenColumnsTitle =>
        HiddenActiveColumnKeys.Count() > 1
            ? $"Dolda filterkolumner: {HiddenColumnsLabel}"
            : $"Dold filterkolumn: {HiddenColumnsLabel}";

    public string HiddenColumnsHelp =>
        HiddenActiveColumnKeys.Count() > 1
            ? "Aktuellt filter använder dolda kolumner."
            : "Aktuellt filter använder en dold kolumn.";

    public void DismissNotice() => _dismissedHiddenSignature = HiddenSignature;

    public void ResetNoticeDismissal() => _dismissedHiddenSignature = null;

    // Make the hidden columns used by active filters visible. Only the column layout
    // changes (counts as a manual edit of the current view); the filter is untouched.
    public async Task RevealColumnsAsync()
    {
        foreach (var key in HiddenActiveColumnKeys.ToList())
            if (_columnExists(key))
                _revealColumn(key);

        _dismissedHiddenSignature = null;
        await _persistColumns();
    }

    // ── Saved filters (personal, per list scope) ────────────────────────
    public List<SavedListFilter> Saved { get; private set; } = [];

    public async Task LoadSavedAsync() => Saved = await _savedStorage.LoadAsync(_scope);

    // Snapshot of the current list state as a (still unnamed) saved filter. Shared by
    // the save action and the duplicate-detection so they compare identical data.
    public SavedListFilter BuildCurrent(string name = "") => new()
    {
        Name = name,
        SortColumn = string.IsNullOrEmpty(_sort.Column) ? null : _sort.Column,
        SortAscending = _sort.Ascending,
        ShowAllVersions = _getShowAllVersions?.Invoke(),
        Filters = _filters
            .Where(kv => kv.Value.IsActive)
            .ToDictionary(kv => kv.Key, kv => SavedColumnFilter.FromState(kv.Value))
    };

    // The first saved filter whose definition matches the current list state, or null.
    // Matching is by normalized content (not name), so a manually rebuilt filter is
    // recognised too.
    public SavedListFilter? Matching
    {
        get
        {
            if (!HasActive) return null;
            var signature = BuildCurrent().ContentSignature();
            return Saved.FirstOrDefault(f => f.ContentSignature() == signature);
        }
    }

    // "Spara aktuellt filter" is only enabled for a non-empty filter that isn't already saved.
    public bool CanSave => HasActive && Matching is null;

    /// <summary>Validates and saves the current filter under <paramref name="name"/>.
    /// Returns null on success, or a user-facing error message.</summary>
    public async Task<string?> SaveAsync(string name)
    {
        name = name.Trim();
        if (name.Length == 0)
            return "Namn krävs.";

        if (Saved.Any(f => string.Equals(f.Name, name, StringComparison.CurrentCultureIgnoreCase)))
            return "Ett filter med detta namn finns redan. Välj ett annat namn.";

        var filter = BuildCurrent(name);
        var signature = filter.ContentSignature();
        if (Saved.Any(f => f.ContentSignature() == signature))
            return "Detta filter är redan sparat.";

        Saved.Add(filter);
        Saved = Saved.OrderBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        await _savedStorage.SaveAllAsync(_scope, Saved);
        return null;
    }

    public void Apply(SavedListFilter saved)
    {
        foreach (var f in _filters.Values) f.Clear();

        foreach (var (key, savedFilter) in saved.Filters)
            if (_filters.TryGetValue(key, out var state) && state.Type == savedFilter.Type)
                savedFilter.ApplyTo(state);

        if (!string.IsNullOrEmpty(saved.SortColumn))
        {
            _sort.Column = saved.SortColumn;
            _sort.Ascending = saved.SortAscending;
        }

        if (saved.ShowAllVersions.HasValue)
            _setShowAllVersions?.Invoke(saved.ShowAllVersions.Value);

        // A freshly applied saved filter must always be able to show the hidden-column
        // notice again, even if the same columns were hidden and the notice was dismissed.
        _dismissedHiddenSignature = null;
        _onChanged();
    }

    public async Task DeleteAsync(SavedListFilter saved)
    {
        Saved.Remove(saved);
        await _savedStorage.SaveAllAsync(_scope, Saved);
    }
}
