using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using ProjectManagement.Client.Helper;

namespace ProjectManagement.Client.Shared.Components;

public class TrimmedNumberInput<TValue> : InputBase<TValue>
{
    [Parameter] public int MaxFractionDigits { get; set; } = NumericFormatHelper.DefaultMaxFractionDigits;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", "text");
        builder.AddAttribute(3, "inputmode", "decimal");
        builder.AddAttribute(4, "name", NameAttributeValue);
        builder.AddAttribute(5, "class", CssClass);
        builder.AddAttribute(6, "value", CurrentValueAsString);
        builder.AddAttribute(7, "onchange", EventCallback.Factory.CreateBinder<string?>(this, value => CurrentValueAsString = value, CurrentValueAsString));
        builder.SetUpdatesAttributeName("value");
        builder.CloseElement();
    }

    protected override string? FormatValueAsString(TValue? value)
    {
        object? number = value;
        if (number is null)
            return string.Empty;

        var culture = CultureInfo.CurrentCulture;
        return number switch
        {
            decimal decimalValue => NumericFormatHelper.Format(decimalValue, MaxFractionDigits, culture),
            double doubleValue => NumericFormatHelper.Format(doubleValue, MaxFractionDigits, culture),
            float floatValue => NumericFormatHelper.Truncate(floatValue, MaxFractionDigits).ToString(NumericFormatHelper.BuildOptionalFractionFormat(MaxFractionDigits), culture),
            _ => base.FormatValueAsString(value)
        };
    }

    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (IsNullable())
            {
                result = default!;
                validationErrorMessage = string.Empty;
                return true;
            }

            result = default!;
            validationErrorMessage = GetParsingErrorMessage();
            return false;
        }

        var normalized = NormalizeNumericInput(value, CultureInfo.CurrentCulture);
        if (TryConvert(normalized, out result))
        {
            validationErrorMessage = string.Empty;
            return true;
        }

        result = default!;
        validationErrorMessage = GetParsingErrorMessage();
        return false;
    }

    private static string NormalizeNumericInput(string value, CultureInfo culture)
    {
        var normalized = value.Trim()
            .Replace("\u00A0", string.Empty)
            .Replace("\u202F", string.Empty)
            .Replace(" ", string.Empty);

        var cultureDecimal = culture.NumberFormat.NumberDecimalSeparator;
        var hasDot = normalized.Contains('.');
        var hasComma = normalized.Contains(',');

        if (hasDot && hasComma)
        {
            var decimalSymbol = normalized.LastIndexOf('.') > normalized.LastIndexOf(',') ? "." : ",";
            var groupSymbol = decimalSymbol == "." ? "," : ".";

            normalized = normalized.Replace(groupSymbol, string.Empty);
            if (decimalSymbol != cultureDecimal)
                normalized = ReplaceLastOccurrence(normalized, decimalSymbol, cultureDecimal);

            return normalized;
        }

        if (hasDot && cultureDecimal != ".")
            return normalized.Replace(".", cultureDecimal);

        if (hasComma && cultureDecimal != ",")
            return normalized.Replace(",", cultureDecimal);

        return normalized;
    }

    private static string ReplaceLastOccurrence(string value, string oldValue, string newValue)
    {
        var index = value.LastIndexOf(oldValue, StringComparison.Ordinal);
        if (index < 0)
            return value;

        return string.Concat(value.AsSpan(0, index), newValue, value.AsSpan(index + oldValue.Length));
    }

    private static bool TryConvert(string value, out TValue result)
    {
        if (BindConverter.TryConvertTo<TValue>(value, CultureInfo.CurrentCulture, out var currentCultureResult))
        {
            result = currentCultureResult!;
            return true;
        }

        if (BindConverter.TryConvertTo<TValue>(value, CultureInfo.InvariantCulture, out var invariantCultureResult))
        {
            result = invariantCultureResult!;
            return true;
        }

        result = default!;
        return false;
    }

    private static bool IsNullable() => Nullable.GetUnderlyingType(typeof(TValue)) is not null;

    private string GetParsingErrorMessage() =>
        $"The {DisplayName ?? FieldIdentifier.FieldName} field must be a number.";
}
