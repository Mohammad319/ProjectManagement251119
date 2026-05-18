using ProjectManagement.Shared.Helper.Text;
using Xunit;

namespace ProjectManagement.Tests;

public class SwedishTaskTextNormalizerTests
{
    [Theory]
    [InlineData("Schakt f\u00f6r grund")]
    [InlineData("Grundschakt")]
    [InlineData("Schaktning f\u00f6r grunder")]
    public void Normalize_MapsCommonGroundExcavationTermsToSameText(string value)
    {
        var actual = SwedishTaskTextNormalizer.Normalize(value);

        AssertContainsTokens(actual, "grund", "schakt");
    }

    [Fact]
    public void Normalize_MapsReinforcementWorkAndWallPlural()
    {
        var actual = SwedishTaskTextNormalizer.Normalize("Armeringsarbete f\u00f6r v\u00e4ggar");

        AssertContainsTokens(actual, "armering", "v\u00e4gg");
    }

    [Fact]
    public void NormalizeTask_IncludesCodeNameAndUnit()
    {
        var actual = SwedishTaskTextNormalizer.NormalizeTask("Grundschakt", "A-10", "kvm");

        AssertContainsTokens(actual, "10", "a", "grund", "m\u00b2", "schakt");
    }

    [Fact]
    public void NormalizeTask_IncludesQuantity()
    {
        var actual = SwedishTaskTextNormalizer.NormalizeTask("Armering i fundament huvudstr\u00e5k", "ESG", "kg", 12.5m);

        Assert.Contains("armering", actual);
        Assert.Contains("esg", actual);
        Assert.Contains("grund", actual);
        Assert.Contains("kg", actual);
        Assert.Contains("12", actual);
        Assert.Contains("5", actual);
    }

    [Fact]
    public void Normalize_DoesNotFilterInternalTaskTypeTokens()
    {
        var actual = SwedishTaskTextNormalizer.Normalize("task fixedq minus codename");

        AssertContainsTokens(actual, "codename", "fixedq", "minus", "task");
    }

    [Theory]
    [InlineData("sch", "Schakt f\u00f6r ledning")]
    [InlineData("betnog", "Betonggjutning")]
    public void CalculateSimilarity_MatchesPrefixesAndMinorTypos(string query, string target)
    {
        var score = FuzzySearchHelper.Score(query, target);

        Assert.True(score >= FuzzySearchHelper.Threshold, $"Expected score above threshold, got {score}");
    }

    private static void AssertContainsTokens(string actual, params string[] expectedTokens)
    {
        var tokens = actual.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var expected in expectedTokens)
            Assert.Contains(expected, tokens);
    }
}
