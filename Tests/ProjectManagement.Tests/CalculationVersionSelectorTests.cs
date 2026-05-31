using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Helper;
using Xunit;

namespace ProjectManagement.Tests;

public class CalculationVersionSelectorTests
{
    [Fact]
    public void SelectCurrentVersions_PrefersMarkedCurrentVersion()
    {
        var familyId = Guid.NewGuid();
        var versions = new[]
        {
            CreateVersion(1, familyId, 1, isCurrent: false),
            CreateVersion(2, familyId, 2, isCurrent: true),
            CreateVersion(3, familyId, 3, isCurrent: false),
        };

        var selected = Assert.Single(CalculationVersionSelector.SelectCurrentVersions(versions));

        Assert.Equal(2, selected.Id);
    }

    [Fact]
    public void SelectCurrentVersions_FallsBackToHighestVersionNumber()
    {
        var familyId = Guid.NewGuid();
        var versions = new[]
        {
            CreateVersion(1, familyId, 1, isCurrent: false),
            CreateVersion(2, familyId, 3, isCurrent: false),
            CreateVersion(3, familyId, 2, isCurrent: false),
        };

        var selected = Assert.Single(CalculationVersionSelector.SelectCurrentVersions(versions));

        Assert.Equal(2, selected.Id);
    }

    [Fact]
    public void SelectCurrentVersions_DoesNotMergeLegacyRowsWithoutFamilyId()
    {
        var versions = new[]
        {
            CreateVersion(1, Guid.Empty, 1, isCurrent: true),
            CreateVersion(2, Guid.Empty, 1, isCurrent: true),
        };

        Assert.Equal(2, CalculationVersionSelector.CountCurrentVersions(versions));
    }

    [Fact]
    public void FilterFamiliesByCurrentVisibility_KeepsHistoryForVisibleCurrentFamily()
    {
        var visibleFamilyId = Guid.NewGuid();
        var archivedFamilyId = Guid.NewGuid();
        var versions = new[]
        {
            CreateVersion(1, visibleFamilyId, 1, isCurrent: false, isVisible: false),
            CreateVersion(2, visibleFamilyId, 2, isCurrent: true, isVisible: true),
            CreateVersion(3, archivedFamilyId, 1, isCurrent: true, isVisible: false),
        };

        var filtered = CalculationVersionSelector
            .FilterFamiliesByCurrentVisibility(versions, includeArchived: false)
            .ToList();

        Assert.Equal([1, 2], filtered.Select(version => version.Id).Order().ToArray());
    }

    private static ListCalculationDTO CreateVersion(
        int id,
        Guid familyId,
        int versionNumber,
        bool isCurrent,
        bool isVisible = true)
    {
        return new ListCalculationDTO
        {
            Id = id,
            VersionGroupId = familyId,
            VersionNumber = versionNumber,
            IsCurrentVersion = isCurrent,
            IsVisible = isVisible,
            CreatedAt = new DateTime(2026, 1, id)
        };
    }
}
