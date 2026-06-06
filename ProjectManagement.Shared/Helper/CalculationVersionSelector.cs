using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Shared.Helper;

public static class CalculationVersionSelector
{
    public static IEnumerable<T> SelectCurrentVersions<T>(IEnumerable<T>? calculations)
        where T : ListCalculationDTO
    {
        return (calculations ?? [])
            .GroupBy(GetFamilyKey)
            .Select(SelectCurrentVersion);
    }

    public static T SelectCurrentVersion<T>(IEnumerable<T> versions)
        where T : ListCalculationDTO
    {
        return versions
            .OrderByDescending(version => version.IsCurrentVersion)
            .ThenByDescending(version => version.VersionNumber > 0 ? version.VersionNumber : int.MinValue)
            .ThenByDescending(version => version.UpdatedAt ?? version.CreatedAt)
            .ThenByDescending(version => version.CreatedAt)
            .ThenByDescending(version => version.Id)
            .First();
    }

    public static int CountCurrentVersions<T>(IEnumerable<T>? calculations)
        where T : ListCalculationDTO
    {
        return SelectCurrentVersions(calculations).Count();
    }

    public static IEnumerable<T> FilterFamiliesByCurrentVisibility<T>(
        IEnumerable<T>? calculations,
        bool includeArchived)
        where T : ListCalculationDTO
    {
        var list = (calculations ?? []).ToList();
        if (includeArchived)
            return list;

        var visibleFamilies = SelectCurrentVersions(list)
            .Where(calculation => !calculation.IsArchived)
            .Select(GetFamilyKey)
            .ToHashSet();

        return list.Where(calculation => visibleFamilies.Contains(GetFamilyKey(calculation)));
    }

    public static bool HasMultipleVersions<T>(IEnumerable<T>? calculations, T calculation)
        where T : ListCalculationDTO
    {
        return (calculations ?? [])
            .Count(version => GetFamilyKey(version) == GetFamilyKey(calculation)) > 1;
    }

    public static IEnumerable<T> GetVersions<T>(IEnumerable<T>? calculations, T calculation)
        where T : ListCalculationDTO
    {
        var familyKey = GetFamilyKey(calculation);
        return (calculations ?? [])
            .Where(version => GetFamilyKey(version) == familyKey)
            .OrderBy(version => version.VersionNumber)
            .ThenBy(version => version.CreatedAt)
            .ThenBy(version => version.Id);
    }

    public static CalculationFamilyKey GetFamilyKey(ListCalculationDTO calculation)
    {
        ArgumentNullException.ThrowIfNull(calculation);

        return calculation.VersionGroupId == Guid.Empty
            ? new CalculationFamilyKey(Guid.Empty, calculation.Id)
            : new CalculationFamilyKey(calculation.VersionGroupId, 0);
    }

    public readonly record struct CalculationFamilyKey(Guid VersionGroupId, int LegacyCalculationId);
}
