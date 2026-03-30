using System.Globalization;

namespace ProjectManagement.Client.Helper;

public static class NumericFormatHelper
{
    public const int DefaultMaxFractionDigits = 4;

    public static string BuildOptionalFractionFormat(int maxFractionDigits)
    {
        var digits = Math.Clamp(maxFractionDigits, 0, 15);
        return digits == 0 ? "0" : $"0.{new string('#', digits)}";
    }

    public static int ExtractMaxFractionDigits(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return 0;

        var separatorIndex = format.IndexOf('.');
        if (separatorIndex < 0 || separatorIndex == format.Length - 1)
            return 0;

        return format.Length - separatorIndex - 1;
    }

    public static decimal Truncate(decimal value, int maxFractionDigits)
    {
        var digits = Math.Clamp(maxFractionDigits, 0, 15);
        if (digits == 0)
            return decimal.Truncate(value);

        var factor = GetDecimalFactor(digits);
        return decimal.Truncate(value * factor) / factor;
    }

    public static double Truncate(double value, int maxFractionDigits)
    {
        var digits = Math.Clamp(maxFractionDigits, 0, 15);
        if (digits == 0)
            return Math.Truncate(value);

        var factor = Math.Pow(10d, digits);
        return Math.Truncate(value * factor) / factor;
    }

    public static float Truncate(float value, int maxFractionDigits)
        => (float)Truncate((double)value, maxFractionDigits);

    public static string Format(decimal value, int maxFractionDigits, CultureInfo? culture = null) =>
        Truncate(value, maxFractionDigits).ToString(BuildOptionalFractionFormat(maxFractionDigits), culture ?? CultureInfo.CurrentCulture);

    public static string Format(decimal? value, int maxFractionDigits, CultureInfo? culture = null) =>
        value.HasValue ? Format(value.Value, maxFractionDigits, culture) : string.Empty;

    public static string Format(double value, int maxFractionDigits, CultureInfo? culture = null) =>
        Truncate(value, maxFractionDigits).ToString(BuildOptionalFractionFormat(maxFractionDigits), culture ?? CultureInfo.CurrentCulture);

    public static string Format(double? value, int maxFractionDigits, CultureInfo? culture = null) =>
        value.HasValue ? Format(value.Value, maxFractionDigits, culture) : string.Empty;

    public static string Format(float value, int maxFractionDigits, CultureInfo? culture = null) =>
        Truncate(value, maxFractionDigits).ToString(BuildOptionalFractionFormat(maxFractionDigits), culture ?? CultureInfo.CurrentCulture);

    public static string Format(float? value, int maxFractionDigits, CultureInfo? culture = null) =>
        value.HasValue ? Format(value.Value, maxFractionDigits, culture) : string.Empty;

    private static decimal GetDecimalFactor(int digits)
    {
        decimal factor = 1m;
        for (var i = 0; i < digits; i++)
            factor *= 10m;

        return factor;
    }
}
