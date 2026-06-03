using Persistence.Seeding;
using Xunit;

namespace ProjectManagement.Tests;

public class ControlStatusSeedTests
{
    private static readonly (string Name, string Code, string Color, bool IsDefault)[] ExpectedStatuses =
    [
        ("Utkast", "DRAFT", "#64748B", true),
        ("Kontroll krävs", "REVIEW_REQUIRED", "#F97316", false),
        ("Mängdkontroll", "QUANTITY_REVIEW", "#F97316", false),
        ("Kostnadskontroll", "COST_REVIEW", "#F97316", false),
        ("Kontrollerad", "REVIEWED", "#16A34A", false),
        ("Avvikande", "DEVIATING", "#DC2626", false),
    ];

    [Fact]
    public void TaskStatusSeeds_MatchControlStatusCatalog()
    {
        AssertControlStatusSeeds(TenantSeedCatalog.TaskStatuses);
    }

    [Fact]
    public void ResourceStatusSeeds_MatchControlStatusCatalog()
    {
        AssertControlStatusSeeds(TenantSeedCatalog.ResourceStatuses);
    }

    private static void AssertControlStatusSeeds(IReadOnlyCollection<LookupSeed> seeds)
    {
        Assert.Equal(ExpectedStatuses.Length, seeds.Count);
        Assert.DoesNotContain(seeds, seed => seed.Name.Contains("Mängd- och kostnadskontroll", StringComparison.OrdinalIgnoreCase));

        foreach (var expected in ExpectedStatuses)
        {
            var seed = Assert.Single(seeds, seed => seed.Code == expected.Code);
            Assert.Equal(expected.Name, seed.Name);
            Assert.Equal(expected.Color.ToLowerInvariant(), seed.Color.ToLowerInvariant());
            Assert.Equal(expected.IsDefault, seed.IsDefault);
            Assert.True(seed.IsSystemDefault);
        }
    }
}
