using System.Globalization;

namespace ProjectManagement.Shared.Helper;

public static class NumericInputHelper
{
    public static string NormalizeNumericInput(string value, CultureInfo? culture = null)
    {
        var activeCulture = culture ?? CultureInfo.CurrentCulture;
        var normalized = value.Trim()
            .Replace("\u00A0", string.Empty)
            .Replace("\u202F", string.Empty)
            .Replace(" ", string.Empty);

        var cultureDecimal = activeCulture.NumberFormat.NumberDecimalSeparator;
        var hasDot = normalized.Contains('.');
        var hasComma = normalized.Contains(',');

        if (hasDot && hasComma)
        {
            var decimalSymbol = normalized.LastIndexOf('.') > normalized.LastIndexOf(',') ? "." : ",";
            var groupSymbol = decimalSymbol == "." ? "," : ".";

            normalized = normalized.Replace(groupSymbol, string.Empty);
            if (decimalSymbol != cultureDecimal)
            {
                normalized = ReplaceLastOccurrence(normalized, decimalSymbol, cultureDecimal);
            }

            return normalized;
        }

        if (hasDot && cultureDecimal != ".")
        {
            return normalized.Replace(".", cultureDecimal);
        }

        if (hasComma && cultureDecimal != ",")
        {
            return normalized.Replace(",", cultureDecimal);
        }

        return normalized;
    }

    public static bool TryParseDecimal(string? text, out decimal value, CultureInfo? culture = null)
    {
        var activeCulture = culture ?? CultureInfo.CurrentCulture;

        if (string.IsNullOrWhiteSpace(text))
        {
            value = default;
            return false;
        }

        var normalized = NormalizeNumericInput(text, activeCulture);

        if (decimal.TryParse(normalized, NumberStyles.Number, activeCulture, out value))
        {
            return true;
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    public static bool TryParseNullableDecimal(string? text, out decimal? value, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = null;
            return true;
        }

        if (TryParseDecimal(text, out var parsed, culture))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    private static string ReplaceLastOccurrence(string value, string oldValue, string newValue)
    {
        var index = value.LastIndexOf(oldValue, StringComparison.Ordinal);
        if (index < 0)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, index), newValue, value.AsSpan(index + oldValue.Length));
    }
}
