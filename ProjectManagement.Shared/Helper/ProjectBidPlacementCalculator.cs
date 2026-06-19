using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Shared.Helper;

public static class ProjectBidPlacementCalculator
{
    public static void Apply(IList<ProjectBidListDTO> bids, BidEvaluationModel model)
    {
        var ranked = bids
            .Where(x => x.IsValid)
            .Select(x => (Bid: x, Value: EvaluationValue(x, model)))
            .Where(x => x.Value.HasValue)
            .OrderBy(x => model == BidEvaluationModel.HighestPoints ? -x.Value!.Value : x.Value!.Value)
            .ToList();

        ApplyCompetitionRanking(ranked, (x, rank) => x.Bid.AutoPlacement = rank);

        foreach (var bid in bids)
            bid.Placement = bid.IsPlacementManuallyOverridden ? bid.ManualPlacement : bid.AutoPlacement;
    }

    public static void Apply(IList<ProjectBidComparisonRowDTO> bids)
    {
        foreach (var projectBids in bids.GroupBy(x => new { x.ProjectId, x.EvaluationModel }))
        {
            var model = projectBids.Key.EvaluationModel;
            var ranked = projectBids
                .Where(x => x.IsValid)
                .Select(x => (Bid: x, Value: model switch
                {
                    BidEvaluationModel.HighestPoints => x.TotalPoints,
                    BidEvaluationModel.LowestTotalCost => x.Amount,
                    _ => x.ComparisonAmount
                }))
                .Where(x => x.Value.HasValue)
                .OrderBy(x => model == BidEvaluationModel.HighestPoints ? -x.Value!.Value : x.Value!.Value)
                .ToList();

            ApplyCompetitionRanking(ranked, (x, rank) => x.Bid.AutoPlacement = rank);

            foreach (var bid in projectBids)
                bid.Placement = bid.IsPlacementManuallyOverridden ? bid.ManualPlacement : bid.AutoPlacement;
        }
    }

    private static decimal? EvaluationValue(ProjectBidListDTO bid, BidEvaluationModel model) =>
        model switch
        {
            BidEvaluationModel.HighestPoints => bid.TotalPoints,
            BidEvaluationModel.LowestTotalCost => bid.Amount,
            _ => bid.ComparisonAmount
        };

    private static void ApplyCompetitionRanking<T>(
        IReadOnlyList<(T Bid, decimal? Value)> ranked,
        Action<(T Bid, decimal? Value), int> setRank)
    {
        decimal? previous = null;
        var rank = 0;

        for (var index = 0; index < ranked.Count; index++)
        {
            if (index == 0 || ranked[index].Value != previous)
                rank = index + 1;

            setRank(ranked[index], rank);
            previous = ranked[index].Value;
        }
    }
}
