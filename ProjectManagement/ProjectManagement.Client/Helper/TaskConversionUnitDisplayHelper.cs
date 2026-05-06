using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Helper.ProjectAppStorage;

namespace ProjectManagement.Client.Helper;

public static class TaskConversionUnitDisplayHelper
{
    public readonly record struct Units(string BaseUnit, string ConvertedUnit);

    public static Units Resolve(TaskMetadata? metadata, string? taskUnit)
    {
        var baseUnit = metadata?.BaseUnit ?? string.Empty;
        var convertedUnit = taskUnit ?? string.Empty;

        if (metadata?.ConversionParameters is not { Count: > 0 })
            return new Units(baseUnit, convertedUnit);

        var hasThickness = HasParameter(metadata, "Thickness");
        var hasWidth = HasParameter(metadata, "Width");
        var hasLength = HasParameter(metadata, "Length");

        var baseKey = UnitRulesCatalog.BuildKey(baseUnit);
        var convertedKey = UnitRulesCatalog.BuildKey(convertedUnit);

        if (hasThickness && !hasWidth && !hasLength && HasUnitPair(baseKey, convertedKey, "m2", "m3"))
            return new Units("m2", "m3");

        if (hasThickness && hasWidth && !hasLength && HasUnitPair(baseKey, convertedKey, "m", "m3"))
            return new Units("m", "m3");

        return new Units(baseUnit, convertedUnit);
    }

    private static bool HasParameter(TaskMetadata metadata, string name)
        => metadata.ConversionParameters.Any(parameter =>
            string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase));

    private static bool HasUnitPair(string first, string second, string unitA, string unitB)
        => (string.Equals(first, unitA, StringComparison.OrdinalIgnoreCase)
            && string.Equals(second, unitB, StringComparison.OrdinalIgnoreCase))
        || (string.Equals(first, unitB, StringComparison.OrdinalIgnoreCase)
            && string.Equals(second, unitA, StringComparison.OrdinalIgnoreCase));
}
