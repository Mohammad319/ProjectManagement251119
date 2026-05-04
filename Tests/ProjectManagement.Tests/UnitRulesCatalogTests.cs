using ProjectManagement.Shared.Helper.ProjectAppStorage;
using Xunit;

namespace ProjectManagement.Tests;

public class UnitRulesCatalogTests
{
    [Fact]
    public void TryGet_NormalizesUnitAliasesBeforeLookup()
    {
        var found = UnitRulesCatalog.TryGet("meter", "m3", out var rule);

        Assert.True(found);
        Assert.Equal(Units.Meter, rule.FromUnit);
        Assert.Equal(Units.CubicMeter, rule.ToUnit);
    }

    [Fact]
    public void LengthToVolume_MultipliesByThicknessAndWidth()
    {
        var parameters = new Dictionary<ParamName, decimal>
        {
            [ParamName.Thickness] = 10m,
            [ParamName.Width] = 20m,
        };

        Assert.True(UnitRulesCatalog.TryGet(Units.Meter, Units.CubicMeter, out var rule));

        var result = rule.Compute(40m, parameters);

        Assert.Equal(8000m, result);
    }

    [Fact]
    public void VolumeToLength_DividesByThicknessAndWidth()
    {
        var parameters = new Dictionary<ParamName, decimal>
        {
            [ParamName.Thickness] = 10m,
            [ParamName.Width] = 20m,
        };

        Assert.True(UnitRulesCatalog.TryGet(Units.CubicMeter, Units.Meter, out var rule));

        var result = rule.Compute(8000m, parameters);

        Assert.Equal(40m, result);
    }
}
