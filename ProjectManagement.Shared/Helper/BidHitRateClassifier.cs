using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Shared.Helper;

public static class BidHitRateClassifier
{
    public static BidHitRateResult ClassifyProject(IEnumerable<ListCalculationDTO>? calculations)
    {
        var currentMainBids = CalculationVersionSelector
            .SelectCurrentVersions(calculations)
            .Where(calculation => calculation.BidRole == BidRole.MainBid)
            .ToList();

        if (!currentMainBids.Any(calculation => calculation.CountsAsSubmittedBid))
            return BidHitRateResult.Excluded;

        if (currentMainBids.Any(calculation => calculation.CountsAsWonBid))
            return BidHitRateResult.Won;

        return currentMainBids.Any(calculation => calculation.CountsAsLostBid)
            ? BidHitRateResult.Lost
            : BidHitRateResult.Undecided;
    }
}

public enum BidHitRateResult
{
    Excluded = 0,
    Won = 1,
    Lost = 2,
    Undecided = 3
}
