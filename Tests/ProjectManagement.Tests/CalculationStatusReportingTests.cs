using Domain.Entities.Project;
using Persistence.Seeding;
using Xunit;

namespace ProjectManagement.Tests;

public class CalculationStatusReportingTests
{
    [Theory]
    [InlineData(false, true, false, true, true, false)]
    [InlineData(false, false, true, true, false, true)]
    [InlineData(true, true, true, true, true, false)]
    public void SetHitRateSettings_EnforcesSubmittedAndMutuallyExclusiveResult(
        bool submitted,
        bool won,
        bool lost,
        bool expectedSubmitted,
        bool expectedWon,
        bool expectedLost)
    {
        var status = new StatusEntity();

        status.SetHitRateSettings(submitted, won, lost);

        Assert.Equal(expectedSubmitted, status.CountsAsSubmittedBid);
        Assert.Equal(expectedWon, status.CountsAsWonBid);
        Assert.Equal(expectedLost, status.CountsAsLostBid);
    }

    [Fact]
    public void CalculationStatusSeeds_IncludeStandardWorkflowStatuses()
    {
        var names = TenantSeedCatalog.CalculationStatuses.Select(status => status.Name).ToHashSet();

        Assert.Contains("Utkast", names);
        Assert.Contains("Planerad", names);
        Assert.Contains("Pågående", names);
        Assert.Contains("Behöver granskas", names);
        Assert.Contains("Granskad", names);
        Assert.Contains("Godkänd / låst", names);
        Assert.Contains("Skickad / inlämnad", names);
        Assert.Contains("Tilldelad / vunnen", names);
        Assert.Contains("Förlorad", names);
        Assert.Contains("Avbruten", names);
        Assert.Contains("Ej intressant / ej lämnat", names);
    }

    [Fact]
    public void CalculationStatusSeeds_HaveExpectedOutcomeFlags()
    {
        var seeds = TenantSeedCatalog.CalculationStatuses.ToDictionary(status => status.Name);

        AssertFlags(seeds["Skickad / inlämnad"], submitted: true);
        AssertFlags(seeds["Tilldelad / vunnen"], submitted: true, won: true);
        AssertFlags(seeds["Förlorad"], submitted: true, lost: true);

        foreach (var seed in seeds.Values.Where(seed =>
                     seed.Name is not "Skickad / inlämnad" and
                     not "Tilldelad / vunnen" and
                     not "Förlorad"))
        {
            AssertFlags(seed);
        }
    }

    private static void AssertFlags(
        CalcStatusSeed seed,
        bool submitted = false,
        bool won = false,
        bool lost = false)
    {
        Assert.Equal(submitted, seed.CountsAsSubmittedBid);
        Assert.Equal(won, seed.CountsAsWonBid);
        Assert.Equal(lost, seed.CountsAsLostBid);
    }
}
