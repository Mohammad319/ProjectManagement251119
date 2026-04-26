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

        Assert.Equal("grund schakt", actual);
    }

    [Fact]
    public void Normalize_MapsReinforcementWorkAndWallPlural()
    {
        var actual = SwedishTaskTextNormalizer.Normalize("Armeringsarbete f\u00f6r v\u00e4ggar");

        Assert.Equal("armering v\u00e4gg", actual);
    }

    [Fact]
    public void NormalizeTask_IncludesCodeNameAndUnit()
    {
        var actual = SwedishTaskTextNormalizer.NormalizeTask("Grundschakt", "A-10", "kvm");

        Assert.Equal("10 a grund m\u00b2 schakt", actual);
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

        Assert.Equal("codename fixedq minus task", actual);
    }
}
