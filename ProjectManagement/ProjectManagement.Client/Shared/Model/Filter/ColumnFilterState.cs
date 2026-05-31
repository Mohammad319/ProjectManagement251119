namespace ProjectManagement.Client.Shared.Model.Filter;

public enum ColumnFilterType { Text, Date, Number, Dropdown, Boolean }
public enum TextFilterOp { Contains, Equals, NotEquals, IsEmpty }
public enum DateFilterOp { On, Before, After, Between, IsEmpty }
public enum NumberFilterOp { Equals, GreaterThan, LessThan, Between, IsEmpty }

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

    public bool IsActive => Type switch
    {
        ColumnFilterType.Text     => TextOp == TextFilterOp.IsEmpty || !string.IsNullOrWhiteSpace(TextValue),
        ColumnFilterType.Date     => DateOp == DateFilterOp.IsEmpty || DateFrom.HasValue,
        ColumnFilterType.Number   => NumberOp == NumberFilterOp.IsEmpty || NumberFrom.HasValue,
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
        return TextOp switch
        {
            TextFilterOp.IsEmpty   => string.IsNullOrWhiteSpace(value),
            TextFilterOp.Contains  => !string.IsNullOrWhiteSpace(value) && value.Contains(TextValue, StringComparison.OrdinalIgnoreCase),
            TextFilterOp.Equals    => string.Equals(value?.Trim(), TextValue.Trim(), StringComparison.OrdinalIgnoreCase),
            TextFilterOp.NotEquals => !string.Equals(value?.Trim(), TextValue.Trim(), StringComparison.OrdinalIgnoreCase),
            _                      => true
        };
    }

    public bool MatchesDate(DateTime? value)
    {
        if (!IsActive) return true;
        bool empty = !value.HasValue || value.Value == default;
        var d = value?.Date;
        return DateOp switch
        {
            DateFilterOp.IsEmpty  => empty,
            DateFilterOp.On       => !empty && DateFrom.HasValue && d!.Value == DateFrom.Value.Date,
            DateFilterOp.Before   => !empty && DateFrom.HasValue && d!.Value < DateFrom.Value.Date,
            DateFilterOp.After    => !empty && DateFrom.HasValue && d!.Value > DateFrom.Value.Date,
            DateFilterOp.Between  => !empty && DateFrom.HasValue && DateTo.HasValue && d!.Value >= DateFrom.Value.Date && d.Value <= DateTo.Value.Date,
            _                     => true
        };
    }

    public bool MatchesNumber(decimal? value)
    {
        if (!IsActive) return true;
        return NumberOp switch
        {
            NumberFilterOp.IsEmpty      => !value.HasValue,
            NumberFilterOp.Equals       => value.HasValue && NumberFrom.HasValue && value.Value == NumberFrom.Value,
            NumberFilterOp.GreaterThan  => value.HasValue && NumberFrom.HasValue && value.Value > NumberFrom.Value,
            NumberFilterOp.LessThan     => value.HasValue && NumberFrom.HasValue && value.Value < NumberFrom.Value,
            NumberFilterOp.Between      => value.HasValue && NumberFrom.HasValue && NumberTo.HasValue && value.Value >= NumberFrom.Value && value.Value <= NumberTo.Value,
            _                           => true
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
}
