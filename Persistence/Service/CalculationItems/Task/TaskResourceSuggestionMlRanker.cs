using Application.Feature.Calculation.Task;
using Microsoft.ML;
using Microsoft.ML.Data;
using ProjectManagement.Shared.Helper.ML;

namespace Persistence.Service.CalculationItems.Task;

public sealed class TaskResourceSuggestionMlRanker : ITaskResourceSuggestionMlRanker
{
    private readonly Lazy<PredictionEngine<ModelInput, ModelOutput>?> predictionEngine;

    public TaskResourceSuggestionMlRanker()
    {
        predictionEngine = new Lazy<PredictionEngine<ModelInput, ModelOutput>?>(CreatePredictionEngine);
    }

    public bool IsEnabled => predictionEngine.Value is not null;

    public double? PredictScore(TaskResourceSuggestionMlFeatures features)
    {
        var engine = predictionEngine.Value;
        if (engine is null)
            return null;

        var prediction = engine.Predict(ModelInput.FromFeatures(features)).Score;
        if (float.IsNaN(prediction) || float.IsInfinity(prediction))
            return null;

        return Math.Clamp(prediction, 0f, 1f);
    }

    private static PredictionEngine<ModelInput, ModelOutput>? CreatePredictionEngine()
    {
        try
        {
            var ml = new MLContext(seed: 251119);
            var modelPath = TaskResourceSuggestionMlModelPath.GetDefaultModelPath();
            if (File.Exists(modelPath))
            {
                using var stream = File.OpenRead(modelPath);
                var savedModel = ml.Model.Load(stream, out _);
                return ml.Model.CreatePredictionEngine<ModelInput, ModelOutput>(savedModel);
            }

            var data = ml.Data.LoadFromEnumerable(BuildBootstrapTrainingData());
            var pipeline = ml.Transforms.Concatenate(
                    "Features",
                    nameof(ModelInput.HeuristicScore),
                    nameof(ModelInput.TextScore),
                    nameof(ModelInput.NameScore),
                    nameof(ModelInput.QuantitySimilarity),
                    nameof(ModelInput.HasQuantitySimilarity),
                    nameof(ModelInput.UnitBonus),
                    nameof(ModelInput.CodeBonus),
                    nameof(ModelInput.HasEquivalentUnits),
                    nameof(ModelInput.HasCompatibleUnits),
                    nameof(ModelInput.IsBlueprintSource))
                .Append(ml.Regression.Trainers.Sdca(
                    labelColumnName: nameof(ModelInput.Label),
                    featureColumnName: "Features"));

            var model = pipeline.Fit(data);
            return ml.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<ModelInput> BuildBootstrapTrainingData()
    {
        var rows = new[]
        {
            new TaskResourceSuggestionMlFeatures(1.00, 1.00, 1.00, 1.00, true, 0.08, 0.15, true, true, true),
            new TaskResourceSuggestionMlFeatures(0.96, 0.82, 1.00, 1.00, true, 0.08, 0.00, true, true, true),
            new TaskResourceSuggestionMlFeatures(0.93, 0.78, 0.92, 0.98, true, 0.06, 0.10, false, true, true),
            new TaskResourceSuggestionMlFeatures(0.86, 0.75, 0.82, 0.85, true, 0.06, 0.00, false, true, false),
            new TaskResourceSuggestionMlFeatures(0.72, 0.68, 0.72, 0.70, true, 0.00, 0.05, false, false, true),
            new TaskResourceSuggestionMlFeatures(0.55, 0.50, 0.55, 0.00, false, 0.00, 0.05, false, false, false),
            new TaskResourceSuggestionMlFeatures(0.42, 0.32, 0.38, 0.95, true, 0.08, 0.00, true, true, false),
            new TaskResourceSuggestionMlFeatures(0.35, 0.25, 0.30, 0.40, true, 0.00, 0.15, false, false, true),
            new TaskResourceSuggestionMlFeatures(0.24, 0.18, 0.20, 0.00, false, 0.00, 0.00, false, false, false),
            new TaskResourceSuggestionMlFeatures(0.12, 0.08, 0.10, 0.10, true, 0.00, 0.00, false, false, false),
        };

        foreach (var row in rows)
        {
            yield return ModelInput.FromFeatures(row) with
            {
                Label = CalculateBootstrapLabel(row)
            };
        }
    }

    private static float CalculateBootstrapLabel(TaskResourceSuggestionMlFeatures row)
    {
        var semantic = (row.TextScore * 0.45d) + (row.NameScore * 0.35d);
        var quantity = row.HasQuantitySimilarity ? row.QuantitySimilarity * 0.12d : 0d;
        var unit = Math.Clamp(row.UnitBonus, -0.04d, 0.15d);
        var code = Math.Min(row.CodeBonus, 0.12d);
        var source = row.IsBlueprintSource ? 0.015d : 0d;
        var heuristicGuard = row.HeuristicScore * 0.20d;
        var label = (semantic * 0.80d) + quantity + unit + code + source + heuristicGuard;

        if (row.TextScore < 0.30d && row.NameScore < 0.30d)
            label = Math.Min(label, 0.45d);

        return (float)Math.Clamp(label, 0d, 1d);
    }

    private sealed record ModelInput
    {
        public float Label { get; init; }
        public float HeuristicScore { get; init; }
        public float TextScore { get; init; }
        public float NameScore { get; init; }
        public float QuantitySimilarity { get; init; }
        public float HasQuantitySimilarity { get; init; }
        public float UnitBonus { get; init; }
        public float CodeBonus { get; init; }
        public float HasEquivalentUnits { get; init; }
        public float HasCompatibleUnits { get; init; }
        public float IsBlueprintSource { get; init; }

        public static ModelInput FromFeatures(TaskResourceSuggestionMlFeatures features)
            => new()
            {
                HeuristicScore = (float)features.HeuristicScore,
                TextScore = (float)features.TextScore,
                NameScore = (float)features.NameScore,
                QuantitySimilarity = (float)features.QuantitySimilarity,
                HasQuantitySimilarity = features.HasQuantitySimilarity ? 1f : 0f,
                UnitBonus = (float)features.UnitBonus,
                CodeBonus = (float)features.CodeBonus,
                HasEquivalentUnits = features.HasEquivalentUnits ? 1f : 0f,
                HasCompatibleUnits = features.HasCompatibleUnits ? 1f : 0f,
                IsBlueprintSource = features.IsBlueprintSource ? 1f : 0f
            };
    }

    private sealed class ModelOutput
    {
        [ColumnName("Score")]
        public float Score { get; set; }
    }
}
