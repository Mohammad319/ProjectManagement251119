using ProjectManagement.Shared.Helper.Text;
using Xunit;

namespace ProjectManagement.Tests;

public class SwedishTaskTextNormalizerTests
{
    [Theory]
    [InlineData("Schakt för grund")]
    [InlineData("Grundschakt")]
    [InlineData("Schaktning för grunder")]
    public void Normalize_MapsCommonGroundExcavationTermsToSameText(string value)
    {
        var actual = SwedishTaskTextNormalizer.Normalize(value);

        Assert.Equal("grund schakt", actual);
    }

    [Fact]
    public void Normalize_MapsReinforcementWorkAndWallPlural()
    {
        var actual = SwedishTaskTextNormalizer.Normalize("Armeringsarbete för väggar");

        Assert.Equal("armering vägg", actual);
    }

    [Fact]
    public void NormalizeTask_IncludesCodeNameAndUnit()
    {
        var actual = SwedishTaskTextNormalizer.NormalizeTask("Grundschakt", "A-10", "kvm");

        Assert.Equal("10 a grund m² schakt", actual);
    }
}
