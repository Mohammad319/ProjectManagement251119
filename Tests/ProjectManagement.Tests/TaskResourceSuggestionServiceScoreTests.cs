using System.Reflection;
using Persistence.Service.CalculationItems.Task;
using ProjectManagement.Shared.Helper.Text;
using Xunit;

namespace ProjectManagement.Tests;

public class TaskResourceSuggestionServiceScoreTests
{
    private const string TaskName = "Armering i fundament, huvudstr\u00e5k";
    private const string Code = "WSG";
    private const string Unit = "kg";
    private const decimal Quantity = 5751m;

    [Fact]
    public void ScoreCandidate_ReturnsDirectMatchForWsgReinforcementTask()
    {
        var normalized = SwedishTaskTextNormalizer.NormalizeTask(TaskName, Code, null);

        var score = ScoreCandidate(
            normalized,
            normalized,
            Unit,
            Unit,
            Code,
            Code,
            Quantity,
            Quantity,
            TaskName,
            TaskName);

        Assert.Equal(1d, score, 4);
    }

    [Fact]
    public void ScoreCandidate_StaysHighWhenStoredTextOnlyContainsName()
    {
        var targetNormalized = SwedishTaskTextNormalizer.NormalizeTask(TaskName, Code, null);
        var olderStoredCandidateNormalized = SwedishTaskTextNormalizer.Normalize(TaskName);

        var score = ScoreCandidate(
            targetNormalized,
            olderStoredCandidateNormalized,
            Unit,
            Unit,
            Code,
            Code,
            Quantity,
            Quantity,
            TaskName,
            TaskName);

        Assert.True(score >= 0.93d);
    }

    [Fact]
    public void ScoreCandidate_StaysHighWhenBlueprintCodeIsMissingButNameQuantityAndUnitMatch()
    {
        var targetNormalized = SwedishTaskTextNormalizer.NormalizeTask(TaskName, Code, null);
        var candidateNormalized = SwedishTaskTextNormalizer.NormalizeTask(TaskName, null, null);

        var score = ScoreCandidate(
            targetNormalized,
            candidateNormalized,
            Unit,
            Unit,
            Code,
            null,
            Quantity,
            Quantity,
            TaskName,
            TaskName);

        Assert.True(score >= 0.96d);
    }

    [Fact]
    public void ScoreCandidate_StaysHighWhenQuantityMatchesAfterUnitConversion()
    {
        var targetNormalized = SwedishTaskTextNormalizer.NormalizeTask(TaskName, Code, null);
        var candidateNormalized = SwedishTaskTextNormalizer.NormalizeTask(TaskName, "ESG.21-0138", null);

        var score = ScoreCandidate(
            targetNormalized,
            candidateNormalized,
            "ton",
            Unit,
            Code,
            "ESG.21-0138",
            5.751m,
            Quantity,
            TaskName,
            TaskName);

        Assert.InRange(score, 0.96d, 0.9899d);
    }

    [Fact]
    public void ScoreCandidate_DoesNotTreatDifferentConvertedQuantityAsDirectMatch()
    {
        var targetNormalized = SwedishTaskTextNormalizer.NormalizeTask(TaskName, Code, null);
        var candidateNormalized = SwedishTaskTextNormalizer.NormalizeTask(TaskName, "ESG.21-0138", null);

        var score = ScoreCandidate(
            targetNormalized,
            candidateNormalized,
            "ton",
            Unit,
            Code,
            "ESG.21-0138",
            5m,
            Quantity,
            TaskName,
            TaskName);

        Assert.True(score < 0.96d, $"Score was {score}.");
    }

    [Fact]
    public void ScoreCandidate_DoesNotTreatSameNameWithDifferentQuantityAsDirectMatch()
    {
        var targetNormalized = SwedishTaskTextNormalizer.NormalizeTask(TaskName, Code, null);
        var candidateNormalized = SwedishTaskTextNormalizer.NormalizeTask(TaskName, null, null);

        var score = ScoreCandidate(
            targetNormalized,
            candidateNormalized,
            Unit,
            Unit,
            Code,
            null,
            Quantity,
            9954m,
            TaskName,
            TaskName);

        Assert.True(score < 0.96d);
    }

    private static double ScoreCandidate(
        string targetNormalized,
        string candidateNormalized,
        string? targetUnit,
        string? candidateUnit,
        string? targetCode,
        string? candidateCode,
        decimal? targetQuantity,
        decimal? candidateQuantity,
        string? targetName,
        string? candidateName)
    {
        var method = typeof(TaskResourceSuggestionService).GetMethod(
            "ScoreCandidate",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (double)method.Invoke(null, [
            targetNormalized,
            candidateNormalized,
            targetUnit,
            candidateUnit,
            targetCode,
            candidateCode,
            targetQuantity,
            candidateQuantity,
            targetName,
            candidateName
        ])!;
    }
}
