using System.Globalization;
using System.Text;

namespace ProjectManagement.Shared.Helper.Text;

/// <summary>
/// Converts simple linear units to a common base unit for comparison.
/// </summary>
public static class QuantityUnitNormalizer
{
    private enum UnitGroup { Count, Mass, Length, Area, Volume }

    private static readonly Dictionary<string, (UnitGroup Group, decimal Factor)> Conversions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Count -> item
            ["st"] = (UnitGroup.Count, 1m),
            ["styck"] = (UnitGroup.Count, 1m),
            ["pcs"] = (UnitGroup.Count, 1m),
            ["pc"] = (UnitGroup.Count, 1m),
            ["piece"] = (UnitGroup.Count, 1m),
            ["pieces"] = (UnitGroup.Count, 1m),

            // Mass -> kg
            ["mg"] = (UnitGroup.Mass, 0.000001m),
            ["g"] = (UnitGroup.Mass, 0.001m),
            ["gr"] = (UnitGroup.Mass, 0.001m),
            ["gram"] = (UnitGroup.Mass, 0.001m),
            ["grams"] = (UnitGroup.Mass, 0.001m),
            ["kg"] = (UnitGroup.Mass, 1m),
            ["kgs"] = (UnitGroup.Mass, 1m),
            ["kilo"] = (UnitGroup.Mass, 1m),
            ["kilos"] = (UnitGroup.Mass, 1m),
            ["kilogram"] = (UnitGroup.Mass, 1m),
            ["kilograms"] = (UnitGroup.Mass, 1m),
            ["kilogramme"] = (UnitGroup.Mass, 1m),
            ["kilogrammes"] = (UnitGroup.Mass, 1m),
            ["\u0643\u063a"] = (UnitGroup.Mass, 1m),
            ["\u0643\u063a\u0645"] = (UnitGroup.Mass, 1m),
            ["\u0643\u062c\u0645"] = (UnitGroup.Mass, 1m),
            ["\u0643\u064a\u0644\u0648"] = (UnitGroup.Mass, 1m),
            ["\u0643\u064a\u0644\u0648\u063a\u0631\u0627\u0645"] = (UnitGroup.Mass, 1m),
            ["\u0643\u064a\u0644\u0648\u062c\u0631\u0627\u0645"] = (UnitGroup.Mass, 1m),
            ["t"] = (UnitGroup.Mass, 1000m),
            ["ton"] = (UnitGroup.Mass, 1000m),
            ["tons"] = (UnitGroup.Mass, 1000m),
            ["tonn"] = (UnitGroup.Mass, 1000m),
            ["tonne"] = (UnitGroup.Mass, 1000m),
            ["tonnes"] = (UnitGroup.Mass, 1000m),
            ["\u0637\u0646"] = (UnitGroup.Mass, 1000m),
            ["\u0627\u0637\u0646\u0627\u0646"] = (UnitGroup.Mass, 1000m),

            // Length -> m
            ["mm"] = (UnitGroup.Length, 0.001m),
            ["millimeter"] = (UnitGroup.Length, 0.001m),
            ["millimeters"] = (UnitGroup.Length, 0.001m),
            ["millimetre"] = (UnitGroup.Length, 0.001m),
            ["millimetres"] = (UnitGroup.Length, 0.001m),
            ["cm"] = (UnitGroup.Length, 0.01m),
            ["centimeter"] = (UnitGroup.Length, 0.01m),
            ["centimeters"] = (UnitGroup.Length, 0.01m),
            ["centimetre"] = (UnitGroup.Length, 0.01m),
            ["centimetres"] = (UnitGroup.Length, 0.01m),
            ["m"] = (UnitGroup.Length, 1m),
            ["meter"] = (UnitGroup.Length, 1m),
            ["meters"] = (UnitGroup.Length, 1m),
            ["metre"] = (UnitGroup.Length, 1m),
            ["metres"] = (UnitGroup.Length, 1m),
            ["km"] = (UnitGroup.Length, 1000m),
            ["kilometer"] = (UnitGroup.Length, 1000m),
            ["kilometers"] = (UnitGroup.Length, 1000m),
            ["kilometre"] = (UnitGroup.Length, 1000m),
            ["kilometres"] = (UnitGroup.Length, 1000m),
            ["mil"] = (UnitGroup.Length, 10000m),

            // Area -> m2
            ["m2"] = (UnitGroup.Area, 1m),
            ["sqm"] = (UnitGroup.Area, 1m),
            ["kvm"] = (UnitGroup.Area, 1m),
            ["squaremeter"] = (UnitGroup.Area, 1m),
            ["squaremeters"] = (UnitGroup.Area, 1m),
            ["squaremetre"] = (UnitGroup.Area, 1m),
            ["squaremetres"] = (UnitGroup.Area, 1m),
            ["kvadratmeter"] = (UnitGroup.Area, 1m),
            ["ha"] = (UnitGroup.Area, 10000m),
            ["hectare"] = (UnitGroup.Area, 10000m),
            ["hectares"] = (UnitGroup.Area, 10000m),
            ["km2"] = (UnitGroup.Area, 1_000_000m),
            ["squarekilometer"] = (UnitGroup.Area, 1_000_000m),
            ["squarekilometers"] = (UnitGroup.Area, 1_000_000m),
            ["squarekilometre"] = (UnitGroup.Area, 1_000_000m),
            ["squarekilometres"] = (UnitGroup.Area, 1_000_000m),

            // Volume -> m3
            ["l"] = (UnitGroup.Volume, 0.001m),
            ["ltr"] = (UnitGroup.Volume, 0.001m),
            ["liter"] = (UnitGroup.Volume, 0.001m),
            ["liters"] = (UnitGroup.Volume, 0.001m),
            ["litre"] = (UnitGroup.Volume, 0.001m),
            ["litres"] = (UnitGroup.Volume, 0.001m),
            ["m3"] = (UnitGroup.Volume, 1m),
            ["kbm"] = (UnitGroup.Volume, 1m),
            ["cbm"] = (UnitGroup.Volume, 1m),
            ["cubicmeter"] = (UnitGroup.Volume, 1m),
            ["cubicmeters"] = (UnitGroup.Volume, 1m),
            ["cubicmetre"] = (UnitGroup.Volume, 1m),
            ["cubicmetres"] = (UnitGroup.Volume, 1m),
            ["kubikmeter"] = (UnitGroup.Volume, 1m),
        };

    public static (decimal Target, decimal Candidate) ToCommonUnit(
        decimal targetQty,
        string? targetUnit,
        decimal candidateQty,
        string? candidateUnit)
    {
        return TryConvertToCommonUnit(targetQty, targetUnit, candidateQty, candidateUnit, out var target, out var candidate)
            ? (target, candidate)
            : (targetQty, candidateQty);
    }

    public static bool TryCalculateQuantitySimilarity(
        decimal targetQty,
        string? targetUnit,
        decimal candidateQty,
        string? candidateUnit,
        out double similarity)
    {
        similarity = 0d;

        if (targetQty <= 0m || candidateQty <= 0m)
            return false;

        if (TryConvertToCommonUnit(targetQty, targetUnit, candidateQty, candidateUnit, out var target, out var candidate))
        {
            similarity = CalculateRatioSimilarity(target, candidate);
            return true;
        }

        var normalizedTargetUnit = NormalizeUnitKey(targetUnit);
        var normalizedCandidateUnit = NormalizeUnitKey(candidateUnit);

        if (string.Equals(normalizedTargetUnit, normalizedCandidateUnit, StringComparison.OrdinalIgnoreCase))
        {
            similarity = CalculateRatioSimilarity(targetQty, candidateQty);
            return true;
        }

        return false;
    }

    public static bool AreEquivalentUnits(string? unit1, string? unit2)
    {
        if (TryGetConversion(unit1, out var conversion1) &&
            TryGetConversion(unit2, out var conversion2))
        {
            return conversion1.Group == conversion2.Group &&
                   conversion1.Factor == conversion2.Factor;
        }

        var normalizedUnit1 = NormalizeUnitKey(unit1);
        var normalizedUnit2 = NormalizeUnitKey(unit2);

        return !string.IsNullOrWhiteSpace(normalizedUnit1) &&
               string.Equals(normalizedUnit1, normalizedUnit2, StringComparison.OrdinalIgnoreCase);
    }

    public static bool AreCompatibleUnits(string? unit1, string? unit2)
    {
        if (!TryGetConversion(unit1, out var conversion1) ||
            !TryGetConversion(unit2, out var conversion2))
        {
            return false;
        }

        return conversion1.Group == conversion2.Group;
    }

    private static bool TryConvertToCommonUnit(
        decimal targetQty,
        string? targetUnit,
        decimal candidateQty,
        string? candidateUnit,
        out decimal target,
        out decimal candidate)
    {
        target = targetQty;
        candidate = candidateQty;

        if (!TryGetConversion(targetUnit, out var targetConversion) ||
            !TryGetConversion(candidateUnit, out var candidateConversion) ||
            targetConversion.Group != candidateConversion.Group)
        {
            return false;
        }

        target = targetQty * targetConversion.Factor;
        candidate = candidateQty * candidateConversion.Factor;
        return true;
    }

    private static bool TryGetConversion(string? unit, out (UnitGroup Group, decimal Factor) conversion)
        => Conversions.TryGetValue(NormalizeUnitKey(unit), out conversion);

    private static double CalculateRatioSimilarity(decimal left, decimal right)
    {
        var smaller = Math.Min(left, right);
        var larger = Math.Max(left, right);

        return larger == 0m
            ? 0d
            : (double)(smaller / larger);
    }

    private static string NormalizeUnitKey(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
            return string.Empty;

        var normalized = unit.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            if (character is '\u00b2')
            {
                builder.Append('2');
                continue;
            }

            if (character is '\u00b3')
            {
                builder.Append('3');
                continue;
            }

            if (char.IsLetterOrDigit(character))
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
