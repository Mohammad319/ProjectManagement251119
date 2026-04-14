using System.Globalization;

namespace BlazorMHD.UI.Shared;

/// <summary>
/// Helpers for localization fallback and direction detection.
/// </summary>
public static class LocaliztionExtensions
{
    private static readonly IReadOnlyDictionary<string, string> Defaults =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SearchPlaceholder"] = "Search...",
            ["NoData"] = "No data available.",
            ["Loading"] = "Loading",
            ["Prev"] = "Prev",
            ["Next"] = "Next",
            ["Page"] = "Page",
            ["Of"] = "of",
            ["RowsPerPage"] = "Rows per page",
            ["SortAsc"] = "Sort ascending",
            ["SortDesc"] = "Sort descending",
            ["Table"] = "Table",
            ["Tree"] = "Tree",
            ["Collapse"] = "Collapse",
            ["Expand"] = "Expand",
            ["Close"] = "Close"
        };

    public static string Get(this IReadOnlyDictionary<string, string>? values, string key)
    {
        if (values is not null && values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return Defaults.TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string GetDirection(bool useCurrentCultureDirection = true)
    {
        if (!useCurrentCultureDirection)
            return "ltr";

        return CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? "rtl" : "ltr";
    }
}
