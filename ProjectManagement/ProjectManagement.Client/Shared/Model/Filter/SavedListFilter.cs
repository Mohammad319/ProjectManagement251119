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
}
