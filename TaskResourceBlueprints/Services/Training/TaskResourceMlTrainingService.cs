using Microsoft.ML;
using ProjectManagement.Shared.Helper.ML;

namespace TaskResourceBlueprints.Services.Training;

public interface ITaskResourceMlTrainingService
{
    Task<TaskResourceMlTrainingResult> TrainAsync(
        int negativeExamplesPerPositive = 2,
        CancellationToken ct = default);
}

public sealed class TaskResourceMlTrainingResult
{
    public bool Trained { get; set; }
    public string ModelPath { get; set; } = string.Empty;
    public int PositiveExamples { get; set; }
    public int NegativeExamples { get; set; }
    public int TotalExamples => PositiveExamples + NegativeExamples;
    public string Message { get; set; } = string.Empty;
}

public sealed class TaskResourceMlTrainingService(ITaskResourceTrainingDatasetService datasetService)
    : ITaskResourceMlTrainingService
{
    public async Task<TaskResourceMlTrainingResult> TrainAsync(
        int negativeExamplesPerPositive = 2,
        CancellationToken ct = default)
    {
        var dataset = await datasetService.BuildAsync(negativeExamplesPerPositive, ct);
        var result = new TaskResourceMlTrainingResult
        {
            ModelPath = TaskResourceSuggestionMlModelPath.GetDefaultModelPath(),
            PositiveExamples = dataset.PositiveExamples,
            NegativeExamples = dataset.NegativeExamples,
        };

        if (dataset.PositiveExamples == 0 || dataset.NegativeExamples == 0)
        {
            result.Message = "Training skipped because both positive and negative examples are required.";
            return result;
        }

        var trainingRows = dataset.Examples.Select(ModelInput.FromTrainingExample).ToList();
        var ml = new MLContext(seed: 251119);
        var data = ml.Data.LoadFromEnumerable(trainingRows);
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
        var directory = Path.GetDirectoryName(result.ModelPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await using var modelStream = File.Create(result.ModelPath);
        ml.Model.Save(model, data.Schema, modelStream);

        result.Trained = true;
        result.Message = $"ML model trained from {result.TotalExamples} examples.";
        return result;
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

        public static ModelInput FromTrainingExample(TaskResourceTrainingExample example)
        {
            var unitBonus = example.HasSameUnit
                ? 0.15d
                : example.HasCompatibleUnit ? 0.10d : -0.04d;
            var heuristic = (example.TextSimilarity * 0.45d) +
                (example.NameSimilarity * 0.30d) +
                (example.QuantitySimilarity * 0.10d) +
                unitBonus +
                0.04d;

            return new ModelInput
            {
                Label = example.Label ? 1f : 0f,
                HeuristicScore = (float)Math.Clamp(heuristic, 0d, 1d),
                TextScore = (float)example.TextSimilarity,
                NameScore = (float)example.NameSimilarity,
                QuantitySimilarity = (float)example.QuantitySimilarity,
                HasQuantitySimilarity = example.HasQuantitySimilarity ? 1f : 0f,
                UnitBonus = (float)unitBonus,
                CodeBonus = 0f,
                HasEquivalentUnits = example.HasSameUnit ? 1f : 0f,
                HasCompatibleUnits = example.HasCompatibleUnit ? 1f : 0f,
                IsBlueprintSource = 1f,
            };
        }
    }
}
