using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Shared.Helper;

public static class BidHitRateClassifier
{
    public static BidHitRateResult ClassifyProject(ListProjectDTO? project)
    {
        if (project is null || !project.CountsAsSubmittedBid)
            return BidHitRateResult.Excluded;

        if (project.CountsAsWonBid)
            return BidHitRateResult.Won;

        return project.CountsAsLostBid
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
