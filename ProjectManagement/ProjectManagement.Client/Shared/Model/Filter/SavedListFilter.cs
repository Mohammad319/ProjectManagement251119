using System.Globalization;
using System.Text;

namespace ProjectManagement.Client.Shared.Model.Filter;

/// <summary>
/// A named, personal filter for the right-panel lists.
/// Project list and calculation list filters are stored under separate
/// scopes and must never be mixed (they have different columns and logic).
/// Column selection/width is intentionally not part of a saved filter.
/// </summary>
public sealed class SavedListFilter
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? SortColumn { get; set; }
    public bool SortAscending { get; set; } = true;

    /// <summary>Calculation list only: saved because it affects which rows the filter selects.</summary>
    public bool? ShowAllVersions { get; set; }

    public Dictionary<string, SavedColumnFilter> Filters { get; set; } = new();

    /// <summary>
    /// A normalized, order-independent signature of the filter definition (the column
    /// filters, sort and show-all-versions flag). Two filters with the same signature
    /// are considered identical regardless of name or the order they were entered in.
    /// Used to prevent saving duplicate filters and to recognise when the current
    /// list state already matches a saved filter. The filter name is intentionally
    /// excluded — matching is by content, not by name.
    /// </summary>
    public string ContentSignature()
    {
        var sb = new StringBuilder();
        foreach (var kv in Filters
                     .Where(kv => kv.Value.IsActive)
                     .OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            sb.Append(kv.Key).Append('=').Append(kv.Value.Signature()).Append(';');
        }
        sb.Append("sort=").Append(SortColumn ?? string.Empty)
          .Append(':').Append(SortAscending ? 'a' : 'd').Append(';');
        sb.Append("ver=").Append(ShowAllVersions?.ToString() ?? string.Empty).Append(';');
        return sb.ToString();
    }
}

/// <summary>Serializable snapshot of one <see cref="ColumnFilterState"/> (operators + values, no available-values cache).</summary>
public sealed class SavedColumnFilter
{
    public ColumnFilterType Type { get; set; }

    public TextFilterOp TextOp { get; set; }
    public string TextValue { get; set; } = string.Empty;

    public DateFilterOp DateOp { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    public NumberFilterOp NumberOp { get; set; }
    public decimal? NumberFrom { get; set; }
    public decimal? NumberTo { get; set; }

    public string BoolValue { get; set; } = string.Empty;

    public List<string> SelectedValues { get; set; } = [];

    public static SavedColumnFilter FromState(ColumnFilterState state) => new()
    {
        Type = state.Type,
        TextOp = state.TextOp,
        TextValue = state.TextValue,
        DateOp = state.DateOp,
        DateFrom = state.DateFrom,
        DateTo = state.DateTo,
        NumberOp = state.NumberOp,
        NumberFrom = state.NumberFrom,
        NumberTo = state.NumberTo,
        BoolValue = state.BoolValue,
        SelectedValues = [.. state.SelectedValues]
    };

    public void ApplyTo(ColumnFilterState state)
    {
        state.TextOp = TextOp;
        state.TextValue = TextValue;
        state.DateOp = DateOp;
        state.DateFrom = DateFrom;
        state.DateTo = DateTo;
        state.NumberOp = NumberOp;
        state.NumberFrom = NumberFrom;
        state.NumberTo = NumberTo;
        state.BoolValue = BoolValue;
        state.SelectedValues = new HashSet<string>(SelectedValues);
    }

    /// <summary>True when this column filter actually constrains the result (mirrors <see cref="ColumnFilterState.IsActive"/>).</summary>
    public bool IsActive => Type switch
    {
        ColumnFilterType.Text     => TextOp is TextFilterOp.IsEmpty or TextFilterOp.IsNotEmpty || !string.IsNullOrWhiteSpace(TextValue),
        ColumnFilterType.Date     => DateOp is DateFilterOp.IsEmpty or DateFilterOp.IsNotEmpty || DateFrom.HasValue,
        ColumnFilterType.Number   => NumberOp is NumberFilterOp.IsEmpty or NumberFilterOp.IsNotEmpty || NumberFrom.HasValue,
        ColumnFilterType.Boolean  => !string.IsNullOrEmpty(BoolValue),
        ColumnFilterType.Dropdown => SelectedValues.Count > 0,
        _                         => false
    };

    /// <summary>
    /// Normalized signature of this single column filter. Only the fields relevant to
    /// the chosen operator are included, text is trimmed/lower-cased, dates reduced to
    /// day precision and dropdown values sorted, so that two semantically identical
    /// filters produce the same signature regardless of how they were entered.
    /// </summary>
    public string Signature() => Type switch
    {
        ColumnFilterType.Text => TextOp is TextFilterOp.IsEmpty or TextFilterOp.IsNotEmpty
            ? $"T:{(int)TextOp}"
            : $"T:{(int)TextOp}:{(TextValue ?? string.Empty).Trim().ToLowerInvariant()}",
        ColumnFilterType.Date => DateOp switch
        {
            DateFilterOp.IsEmpty or DateFilterOp.IsNotEmpty => $"D:{(int)DateOp}",
            DateFilterOp.Between => $"D:{(int)DateOp}:{DateKey(DateFrom)}:{DateKey(DateTo)}",
            _ => $"D:{(int)DateOp}:{DateKey(DateFrom)}"
        },
        ColumnFilterType.Number => NumberOp switch
        {
            NumberFilterOp.IsEmpty or NumberFilterOp.IsNotEmpty => $"N:{(int)NumberOp}",
            NumberFilterOp.Between => $"N:{(int)NumberOp}:{NumberKey(NumberFrom)}:{NumberKey(NumberTo)}",
            _ => $"N:{(int)NumberOp}:{NumberKey(NumberFrom)}"
        },
        ColumnFilterType.Boolean => $"B:{BoolValue}",
        ColumnFilterType.Dropdown =>
            $"L:{string.Join(",", SelectedValues.Select(v => (v ?? string.Empty).Trim()).OrderBy(v => v, StringComparer.Ordinal))}",
        _ => string.Empty
    };

    private static string DateKey(DateTime? value) =>
        value?.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string NumberKey(decimal? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
}
