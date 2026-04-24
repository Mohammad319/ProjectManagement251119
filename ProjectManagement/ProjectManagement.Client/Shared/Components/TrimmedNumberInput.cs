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
            result = default!;
            validationErrorMessage = string.Empty;
            return true;
        }

        var culture = CultureInfo.CurrentCulture;

        // Try math expression first: handles +, -, *, / and plain a/b fractions
        if (TryEvaluateExpression(value.Trim(), culture, out var expressionResult))
        {
            var exprStr = expressionResult.ToString(CultureInfo.InvariantCulture);
            if (TryConvert(exprStr, out result))
            {
                validationErrorMessage = string.Empty;
                return true;
            }
        }

        // Fallback: plain number
        var normalized = NormalizeNumericInput(value, culture);
        if (TryConvert(normalized, out result))
        {
            validationErrorMessage = string.Empty;
            return true;
        }

        result = default!;
        validationErrorMessage = GetParsingErrorMessage();
        return false;
    }

    // -------------------------------------------------------
    // Math expression evaluator  (+  −  *  /)
    // Supports: "5+6"  "3-1"  "10/2"  "2*3"  "1.5+2*3"
    // -------------------------------------------------------

    private static bool TryEvaluateExpression(string value, CultureInfo culture, out decimal result)
    {
        result = 0m;

        // Detect at least one arithmetic operator (skip leading sign)
        bool hasOp = false;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (i > 0 && (c == '+' || c == '-' || c == '*' || c == '/'))
            {
                hasOp = true;
                break;
            }
        }

        if (!hasOp)
            return false;

        try
        {
            int pos = 0;
            result = ParseAddSub(value, culture, ref pos);
            SkipSpaces(value, ref pos);
            return pos >= value.Length;
        }
        catch
        {
            return false;
        }
    }

    // Level 1: addition and subtraction (lowest precedence)
    private static decimal ParseAddSub(string expr, CultureInfo culture, ref int pos)
    {
        var left = ParseMulDiv(expr, culture, ref pos);
        SkipSpaces(expr, ref pos);

        while (pos < expr.Length && (expr[pos] == '+' || expr[pos] == '-'))
        {
            char op = expr[pos++];
            var right = ParseMulDiv(expr, culture, ref pos);
            left = op == '+' ? left + right : left - right;
            SkipSpaces(expr, ref pos);
        }

        return left;
    }

    // Level 2: multiplication and division (higher precedence)
    private static decimal ParseMulDiv(string expr, CultureInfo culture, ref int pos)
    {
        var left = ParseNumber(expr, culture, ref pos);
        SkipSpaces(expr, ref pos);

        while (pos < expr.Length && (expr[pos] == '*' || expr[pos] == '/'))
        {
            char op = expr[pos++];
            var right = ParseNumber(expr, culture, ref pos);
            if (op == '/' && right == 0m) throw new DivideByZeroException();
            left = op == '*' ? left * right : left / right;
            SkipSpaces(expr, ref pos);
        }

        return left;
    }

    // Level 3: a single numeric literal with optional leading sign
    private static decimal ParseNumber(string expr, CultureInfo culture, ref int pos)
    {
        SkipSpaces(expr, ref pos);

        bool negative = false;
        if (pos < expr.Length && expr[pos] == '-') { negative = true; pos++; }
        else if (pos < expr.Length && expr[pos] == '+') { pos++; }

        SkipSpaces(expr, ref pos);

        int start = pos;
        while (pos < expr.Length && (char.IsDigit(expr[pos]) || expr[pos] == '.' || expr[pos] == ','))
            pos++;

        if (pos == start) throw new FormatException("Expected a number.");

        var numStr = expr[start..pos];
        var normalized = NormalizeNumericInput(numStr, culture);

        if (!decimal.TryParse(normalized, NumberStyles.Any, culture, out var num) &&
            !decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
            throw new FormatException($"Cannot parse '{numStr}'.");

        return negative ? -num : num;
    }

    private static void SkipSpaces(string expr, ref int pos)
    {
        while (pos < expr.Length && expr[pos] == ' ') pos++;
    }

    // -------------------------------------------------------
    // Helpers (unchanged)
    // -------------------------------------------------------

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

    private string GetParsingErrorMessage() =>
        $"The {DisplayName ?? FieldIdentifier.FieldName} field must be a number.";
}
