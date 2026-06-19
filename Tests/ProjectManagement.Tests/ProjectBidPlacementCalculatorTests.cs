using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper;
using Xunit;

namespace ProjectManagement.Tests;

public class ProjectBidPlacementCalculatorTests
{
    [Fact]
    public void Apply_LowestComparison_UsesCompetitionRankingForTies()
    {
        var bids = new List<ProjectBidListDTO>
        {
            PriceBid(1, 50_000),
            PriceBid(2, 50_000),
            PriceBid(3, 60_000)
        };

        ProjectBidPlacementCalculator.Apply(bids, BidEvaluationModel.LowestComparison);

        Assert.Equal([1, 1, 3], bids.Select(x => x.Placement));
    }

    [Fact]
    public void Apply_HighestPoints_RanksDescending()
    {
        var bids = new List<ProjectBidListDTO>
        {
            new() { Id = 1, TotalPoints = 72 },
            new() { Id = 2, TotalPoints = 95 },
            new() { Id = 3, TotalPoints = 80 }
        };

        ProjectBidPlacementCalculator.Apply(bids, BidEvaluationModel.HighestPoints);

        Assert.Equal([3, 1, 2], bids.Select(x => x.Placement));
    }

    [Fact]
    public void Apply_ManualPlacement_OverridesOnlyFinalPlacement()
    {
        var bids = new List<ProjectBidListDTO>
        {
            PriceBid(1, 50_000),
            new()
            {
                Id = 2,
                Amount = 60_000,
                ManualPlacement = 1,
                IsPlacementManuallyOverridden = true
            }
        };

        ProjectBidPlacementCalculator.Apply(bids, BidEvaluationModel.LowestComparison);

        Assert.Equal(2, bids[1].AutoPlacement);
        Assert.Equal(1, bids[1].Placement);
        Assert.False(bids[1].IsAwarded);
    }

    [Fact]
    public void Apply_LowestTotalCost_UsesAmountWithoutDeduction()
    {
        var bids = new List<ProjectBidListDTO>
        {
            new() { Id = 1, Amount = 100, DeductionPercent = 50 },
            new() { Id = 2, Amount = 80 }
        };

        ProjectBidPlacementCalculator.Apply(bids, BidEvaluationModel.LowestTotalCost);

        Assert.Equal([2, 1], bids.Select(x => x.Placement));
    }

    private static ProjectBidListDTO PriceBid(int id, decimal amount) =>
        new() { Id = id, Amount = amount };
}
