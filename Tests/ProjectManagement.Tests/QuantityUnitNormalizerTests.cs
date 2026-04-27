using ProjectManagement.Shared.Helper.Text;
using Xunit;

namespace ProjectManagement.Tests;

public class QuantityUnitNormalizerTests
{
    [Theory]
    [InlineData(5000, "kg", 5, "ton")]
    [InlineData(5000, "kilogram", 5, "t")]
    [InlineData(5000, "\u0643\u064a\u0644\u0648 \u063a\u0631\u0627\u0645", 5, "\u0637\u0646")]
    [InlineData(1000, "m", 1, "km")]
    [InlineData(10000, "m2", 1, "ha")]
    [InlineData(1000, "l", 1, "m3")]
    public void TryCalculateQuantitySimilarity_ConvertsCompatibleUnits(
        int targetQuantity,
        string targetUnit,
        int candidateQuantity,
        string candidateUnit)
    {
        var comparable = QuantityUnitNormalizer.TryCalculateQuantitySimilarity(
            targetQuantity,
            targetUnit,
            candidateQuantity,
            candidateUnit,
            out var similarity);

        Assert.True(comparable);
        Assert.Equal(1d, similarity, 4);
    }

    [Theory]
    [InlineData("kg", "kilogram")]
    [InlineData("k.g.", "kg")]
    [InlineData("m\u00b2", "m2")]
    [InlineData("\u0643\u063a", "\u0643\u064a\u0644\u0648\u063a\u0631\u0627\u0645")]
    public void AreEquivalentUnits_RecognizesAliases(string left, string right)
    {
        Assert.True(QuantityUnitNormalizer.AreEquivalentUnits(left, right));
    }

    [Fact]
    public void TryCalculateQuantitySimilarity_DoesNotCompareDifferentUnknownUnits()
    {
        var comparable = QuantityUnitNormalizer.TryCalculateQuantitySimilarity(
            5000m,
            "bag",
            5m,
            "sack",
            out _);

        Assert.False(comparable);
    }

    [Fact]
    public void TryCalculateQuantitySimilarity_ComparesSameUnknownUnits()
    {
        var comparable = QuantityUnitNormalizer.TryCalculateQuantitySimilarity(
            5m,
            "bag",
            5m,
            "bag",
            out var similarity);

        Assert.True(comparable);
        Assert.Equal(1d, similarity, 4);
    }
}
