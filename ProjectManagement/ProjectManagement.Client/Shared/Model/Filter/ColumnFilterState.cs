namespace ProjectManagement.Client.Shared.Model.Filter;

public enum ColumnFilterType { Text, Date, Number, Dropdown, Boolean }

/// <summary>One selectable option in a grouped dropdown filter (display label decoupled from match value).</summary>
public sealed class FilterOption
{
    public required string Value { get; init; }
    public required string Label { get; init; }
}

/// <summary>A headed section of <see cref="FilterOption"/>s in a grouped dropdown filter.</summary>
public sealed class FilterOptionGroup
{
    public required string Header { get; init; }
    public required List<FilterOption> Options { get; init; }
}
public enum TextFilterOp  { Contains, Equals, NotEquals, IsEmpty, StartsWith, EndsWith, Like, IsNotEmpty }
public enum DateFilterOp  { On, Before, After, Between, IsEmpty, IsNotEmpty }
public enum NumberFilterOp { Equals, GreaterThan, LessThan, Between, IsEmpty, NotEquals, IsNotEmpty }

public class ColumnFilterState
{
    public required ColumnFilterType Type { get; init; }

    public TextFilterOp TextOp { get; set; } = TextFilterOp.Contains;
    public string TextValue { get; set; } = string.Empty;

    public DateFilterOp DateOp { get; set; } = DateFilterOp.On;
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    public NumberFilterOp NumberOp { get; set; } = NumberFilterOp.Equals;
    public decimal? NumberFrom { get; set; }
    public decimal? NumberTo { get; set; }

    public string BoolValue { get; set; } = string.Empty;

    public HashSet<string> SelectedValues { get; set; } = [];
    public List<string> AvailableValues { get; set; } = [];

    /// <summary>
    /// Optional grouped representation for a dropdown filter. When set (non-empty) the popup
    /// renders headed sections with thin dividers instead of a single flat list. Each option's
    /// <see cref="FilterOption.Value"/> is what ends up in <see cref="SelectedValues"/> and is
    /// matched against the row tags; <see cref="FilterOption.Label"/> is what the user sees.
    /// </summary>
    public List<FilterOptionGroup>? AvailableGroups { get; set; }

    public bool IsActive => Type switch
    {
        ColumnFilterType.Text     => TextOp is TextFilterOp.IsEmpty or TextFilterOp.IsNotEmpty || !string.IsNullOrWhiteSpace(TextValue),
        ColumnFilterType.Date     => DateOp is DateFilterOp.IsEmpty or DateFilterOp.IsNotEmpty || DateFrom.HasValue,
        ColumnFilterType.Number   => NumberOp is NumberFilterOp.IsEmpty or NumberFilterOp.IsNotEmpty || NumberFrom.HasValue,
        ColumnFilterType.Boolean  => !string.IsNullOrEmpty(BoolValue),
        ColumnFilterType.Dropdown => SelectedValues.Count > 0,
        _                         => false
    };

    public void Clear()
    {
        TextOp = TextFilterOp.Contains; TextValue = string.Empty;
        DateOp = DateFilterOp.On;       DateFrom = null; DateTo = null;
        NumberOp = NumberFilterOp.Equals; NumberFrom = null; NumberTo = null;
        BoolValue = string.Empty;
        SelectedValues.Clear();
    }

    public bool MatchesText(string? value)
    {
        if (!IsActive) return true;
        var v = value?.Trim() ?? string.Empty;
        var f = TextValue.Trim();
        return TextOp switch
        {
            TextFilterOp.IsEmpty    => string.IsNullOrWhiteSpace(value),
            TextFilterOp.IsNotEmpty => !string.IsNullOrWhiteSpace(value),
            TextFilterOp.Contains   => v.Contains(f, StringComparison.OrdinalIgnoreCase),
            TextFilterOp.Equals     => string.Equals(v, f, StringComparison.OrdinalIgnoreCase),
            TextFilterOp.NotEquals  => !string.Equals(v, f, StringComparison.OrdinalIgnoreCase),
            TextFilterOp.StartsWith => v.StartsWith(f, StringComparison.OrdinalIgnoreCase),
            TextFilterOp.EndsWith   => v.EndsWith(f, StringComparison.OrdinalIgnoreCase),
            TextFilterOp.Like       => FuzzyMatch(v, f),
            _                       => true
        };
    }

    public bool MatchesDate(DateTime? value)
    {
        if (!IsActive) return true;
        bool empty = !value.HasValue || value.Value == default;
        var d = value?.Date;
        return DateOp switch
        {
            DateFilterOp.IsEmpty    => empty,
            DateFilterOp.IsNotEmpty => !empty,
            DateFilterOp.On         => !empty && DateFrom.HasValue && d!.Value == DateFrom.Value.Date,
            DateFilterOp.Before     => !empty && DateFrom.HasValue && d!.Value < DateFrom.Value.Date,
            DateFilterOp.After      => !empty && DateFrom.HasValue && d!.Value > DateFrom.Value.Date,
            DateFilterOp.Between    => !empty && DateFrom.HasValue && DateTo.HasValue && d!.Value >= DateFrom.Value.Date && d.Value <= DateTo.Value.Date,
            _                       => true
        };
    }

    public bool MatchesNumber(decimal? value)
    {
        if (!IsActive) return true;
        return NumberOp switch
        {
            NumberFilterOp.IsEmpty     => !value.HasValue,
            NumberFilterOp.IsNotEmpty  => value.HasValue,
            NumberFilterOp.Equals      => value.HasValue && NumberFrom.HasValue && value.Value == NumberFrom.Value,
            NumberFilterOp.NotEquals   => value.HasValue && NumberFrom.HasValue && value.Value != NumberFrom.Value,
            NumberFilterOp.GreaterThan => value.HasValue && NumberFrom.HasValue && value.Value > NumberFrom.Value,
            NumberFilterOp.LessThan    => value.HasValue && NumberFrom.HasValue && value.Value < NumberFrom.Value,
            NumberFilterOp.Between     => value.HasValue && NumberFrom.HasValue && NumberTo.HasValue && value.Value >= NumberFrom.Value && value.Value <= NumberTo.Value,
            _                          => true
        };
    }

    public bool MatchesBool(bool value)
    {
        if (!IsActive) return true;
        return BoolValue switch { "true" => value, "false" => !value, _ => true };
    }

    public bool MatchesDropdown(string? value)
    {
        if (!IsActive) return true;
        return SelectedValues.Contains(value ?? string.Empty);
    }

    // Fuzzy match: substring check first, then Levenshtein with relative threshold.
    private static bool FuzzyMatch(string value, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;
        var v = value.ToLowerInvariant();
        var f = filter.ToLowerInvariant();
        if (v.Contains(f)) return true;
        int threshold = Math.Max(1, f.Length / 3);
        return LevenshteinDistance(v, f) <= threshold;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int n = a.Length, m = b.Length;
        if (n == 0) return m;
        if (m == 0) return n;
        var dp = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) dp[i, 0] = i;
        for (int j = 0; j <= m; j++) dp[0, j] = j;
        for (int i = 1; i <= n; i++)
            for (int j = 1; j <= m; j++)
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return dp[n, m];
    }
}
