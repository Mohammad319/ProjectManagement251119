namespace Application.Feature.Calculation.Task;

public interface ITaskResourceSuggestionMlRanker
{
    bool IsEnabled { get; }
    double? PredictScore(TaskResourceSuggestionMlFeatures features);
}

public sealed record TaskResourceSuggestionMlFeatures(
    double HeuristicScore,
    double TextScore,
    double NameScore,
    double QuantitySimilarity,
    bool HasQuantitySimilarity,
    double UnitBonus,
    double CodeBonus,
    bool HasEquivalentUnits,
    bool HasCompatibleUnits,
    bool IsBlueprintSource);
