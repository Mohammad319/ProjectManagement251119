using System.Globalization;

namespace ProjectManagement.Shared.Helper;

public static class NumericDisplayHelper
{
    private const string DecimalFormat = "0.#############################";
    private const string FloatingPointFormat = "0.################";

    public static string Format(decimal value, CultureInfo? culture = null)
        => value.ToString(DecimalFormat, culture ?? CultureInfo.CurrentCulture);

    public static string Format(decimal? value, CultureInfo? culture = null)
        => value.HasValue ? Format(value.Value, culture) : string.Empty;
}
