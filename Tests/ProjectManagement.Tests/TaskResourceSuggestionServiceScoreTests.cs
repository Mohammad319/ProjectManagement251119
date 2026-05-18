using System.Reflection;
using Application.Feature.Calculation.Task;
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

    [Fact]
    public void ScoreCandidate_KeepsMinorTypoAsUsefulSuggestion()
    {
        var targetName = "Schakt f\u00f6r ledning";
        var candidateName = "Scahkt ledning";
        var targetNormalized = SwedishTaskTextNormalizer.NormalizeTask(targetName, "CBB", null);
        var candidateNormalized = SwedishTaskTextNormalizer.NormalizeTask(candidateName, "CBB", null);

        var score = ScoreCandidate(
            targetNormalized,
            candidateNormalized,
            "m3",
            "m3",
            "CBB",
            "CBB",
            120m,
            120m,
            targetName,
            candidateName);

        Assert.True(score >= 0.75d, $"Score was {score}.");
    }

    [Fact]
    public void ScoreCandidate_DoesNotRankCodeAloneAsStrongMatch()
    {
        var score = ScoreCandidate(
            SwedishTaskTextNormalizer.NormalizeTask("Schakt f\u00f6r ledning", "CBB", null),
            SwedishTaskTextNormalizer.NormalizeTask("M\u00e5lning av v\u00e4gg", "CBB", null),
            "m3",
            "m2",
            "CBB",
            "CBB",
            120m,
            30m,
            "Schakt f\u00f6r ledning",
            "M\u00e5lning av v\u00e4gg");

        Assert.True(score < 0.55d, $"Score was {score}.");
    }

    [Fact]
    public void ExplainCandidateScore_ReturnsReasonInputsForDiagnostics()
    {
        var breakdown = ExplainCandidateScore(
            SwedishTaskTextNormalizer.NormalizeTask(TaskName, Code, null),
            SwedishTaskTextNormalizer.NormalizeTask(TaskName, Code, null),
            Unit,
            Unit,
            Code,
            Code,
            Quantity,
            Quantity,
            TaskName,
            TaskName);

        Assert.True(GetDouble(breakdown, "TextScore") > 0.9d);
        Assert.True(GetDouble(breakdown, "NameScore") > 0.9d);
        Assert.True(GetDouble(breakdown, "CodeBonus") > 0d);
        Assert.True(GetNullableDouble(breakdown, "QuantitySimilarity") > 0.99d);

        var reason = BuildReason(breakdown);

        Assert.Contains("name", reason);
        Assert.Contains("code", reason);
        Assert.Contains("same unit", reason);
        Assert.Contains("quantity", reason);
    }

    [Fact]
    public void ExplainCandidateScore_GivesStrongerWeightToUnits()
    {
        var targetName = "Schakt for ledning";
        var candidateName = "Schakt ledning";
        var targetNormalized = SwedishTaskTextNormalizer.NormalizeTask(targetName, "CBB.311", null);
        var candidateNormalized = SwedishTaskTextNormalizer.NormalizeTask(candidateName, "CBB.311", null);

        var sameUnit = ExplainCandidateScore(
            targetNormalized,
            candidateNormalized,
            "m3",
            "m3",
            "CBB.311",
            "CBB.311",
            null,
            null,
            targetName,
            candidateName);

        var compatibleUnit = ExplainCandidateScore(
            targetNormalized,
            candidateNormalized,
            "ton",
            "kg",
            "CBB.311",
            "CBB.311",
            null,
            null,
            targetName,
            candidateName);

        var differentUnit = ExplainCandidateScore(
            targetNormalized,
            candidateNormalized,
            "m3",
            "m2",
            "CBB.311",
            "CBB.311",
            null,
            null,
            targetName,
            candidateName);

        Assert.Equal(0.15d, GetDouble(sameUnit, "UnitBonus"), 4);
        Assert.Equal(0.10d, GetDouble(compatibleUnit, "UnitBonus"), 4);
        Assert.Equal(-0.04d, GetDouble(differentUnit, "UnitBonus"), 4);
        Assert.True(GetDouble(sameUnit, "Score") > GetDouble(differentUnit, "Score"));
        Assert.True(GetDouble(compatibleUnit, "Score") > GetDouble(differentUnit, "Score"));
    }

    [Fact]
    public void ExplainCandidateScore_DiscountsInheritedParentCode()
    {
        var targetName = "Gr\u00f6na ytor, markklass 2";
        var candidateName = "Jordschakt f\u00f6r el- och telekabel";
        var targetNormalized = SwedishTaskTextNormalizer.Normalize("CBB.32 Jordschakt f\u00f6r el- och telekabel Gr\u00f6na ytor markklass");
        var candidateNormalized = SwedishTaskTextNormalizer.Normalize("CBB.32 Jordschakt f\u00f6r el- och telekabel massor markklass");

        var ownCode = ExplainCandidateScoreWithCodeContext(
            targetNormalized,
            candidateNormalized,
            "m3",
            "m3",
            "CBB.32",
            "CBB.32",
            null,
            null,
            targetName,
            candidateName,
            0,
            0);

        var inheritedCandidateCode = ExplainCandidateScoreWithCodeContext(
            targetNormalized,
            candidateNormalized,
            "m3",
            "m3",
            "CBB.32",
            "CBB.32",
            null,
            null,
            targetName,
            candidateName,
            0,
            1);

        Assert.True(GetDouble(ownCode, "CodeBonus") > GetDouble(inheritedCandidateCode, "CodeBonus"));
        Assert.Equal(GetDouble(ownCode, "CodeBonus") * 0.75d, GetDouble(inheritedCandidateCode, "CodeBonus"), 4);
    }

    [Fact]
    public void BuildReason_IncludesParentContextDiagnostics()
    {
        var breakdown = ExplainCandidateScoreWithCodeContext(
            SwedishTaskTextNormalizer.Normalize("CBB.32 Jordschakt Gr\u00f6na ytor"),
            SwedishTaskTextNormalizer.Normalize("CBB.32 Jordschakt Massor"),
            "m3",
            "m3",
            "CBB.32",
            "CBB.32",
            null,
            null,
            "Gr\u00f6na ytor",
            "Massor",
            1,
            0);

        breakdown.GetType().GetProperty("ContextReason")!.SetValue(breakdown, "target uses parent code CBB.32");
        var reason = BuildReason(breakdown);

        Assert.Contains("parent code CBB.32", reason);
    }

    [Fact]
    public void MlRanker_PredictsBoundedScoreFromBreakdownFeatures()
    {
        var ranker = new TaskResourceSuggestionMlRanker();

        var score = ranker.PredictScore(new TaskResourceSuggestionMlFeatures(
            HeuristicScore: 0.90d,
            TextScore: 0.82d,
            NameScore: 0.88d,
            QuantitySimilarity: 0.95d,
            HasQuantitySimilarity: true,
            UnitBonus: 0.06d,
            CodeBonus: 0.10d,
            HasEquivalentUnits: false,
            HasCompatibleUnits: true,
            IsBlueprintSource: true));

        Assert.True(ranker.IsEnabled);
        Assert.NotNull(score);
        Assert.InRange(score.Value, 0d, 1d);
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

    private static object ExplainCandidateScore(
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
            "ExplainCandidateScore",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return method.Invoke(null, [
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

    private static string BuildReason(object breakdown)
    {
        var method = typeof(TaskResourceSuggestionService).GetMethod(
            "BuildReason",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (string)method.Invoke(null, [breakdown, ProjectManagement.Shared.DTO.Calculation.TaskResourceSuggestionSource.BlueprintTask])!;
    }

    private static object ExplainCandidateScoreWithCodeContext(
        string targetNormalized,
        string candidateNormalized,
        string? targetUnit,
        string? candidateUnit,
        string? targetCode,
        string? candidateCode,
        decimal? targetQuantity,
        decimal? candidateQuantity,
        string? targetName,
        string? candidateName,
        int targetCodeDepth,
        int candidateCodeDepth)
    {
        var method = typeof(TaskResourceSuggestionService).GetMethod(
            "ExplainCandidateScoreWithCodeContext",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return method.Invoke(null, [
            targetNormalized,
            candidateNormalized,
            targetUnit,
            candidateUnit,
            targetCode,
            candidateCode,
            targetQuantity,
            candidateQuantity,
            targetName,
            candidateName,
            targetCodeDepth,
            candidateCodeDepth
        ])!;
    }

    private static double GetDouble(object source, string propertyName)
        => (double)source.GetType().GetProperty(propertyName)!.GetValue(source)!;

    private static double? GetNullableDouble(object source, string propertyName)
        => (double?)source.GetType().GetProperty(propertyName)!.GetValue(source);
}
