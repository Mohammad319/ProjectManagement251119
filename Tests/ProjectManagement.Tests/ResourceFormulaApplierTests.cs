using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Helper.ProjectAppStorage;
using Xunit;

namespace ProjectManagement.Tests;

public class ResourceFormulaApplierTests
{
    [Fact]
    public void ApplyRow_UsesPropertyDisplayNameAliasToUpdateBaseCost()
    {
        var row = new ResourceDto
        {
            Data = new ResourceMetadata
            {
                BaseCost = 600m,
                Cost = 650m,
                ChangeFactor1 = 1m,
                ChangeFactor2 = 1m
            },
            Formulas = ["basecost=P1"],
            Properties =
            [
                new ResourcePropertyBindDto
                {
                    Id = 55,
                    DisplayName = "P1",
                    DataType = DataType.Number,
                    NumberDefault = 10m
                }
            ]
        };

        ResourceFormulaApplier.ApplyRow(row, new Dictionary<ParamName, decimal>());

        Assert.Equal(10m, row.Data.BaseCost);
    }

    [Fact]
    public void ApplyRow_UsesLegacyChangeFactorAliasInsideExpression()
    {
        var row = new ResourceDto
        {
            Data = new ResourceMetadata
            {
                BaseCost = 600m,
                Cost = 650m,
                ChangeFactor1 = 3m,
                ChangeFactor2 = 1m
            },
            Formulas = ["basecost=chf1+P1"],
            Properties =
            [
                new ResourcePropertyBindDto
                {
                    Id = 55,
                    DisplayName = "P1",
                    DataType = DataType.Number,
                    NumberDefault = 10m
                }
            ]
        };

        ResourceFormulaApplier.ApplyRow(row, new Dictionary<ParamName, decimal>());

        Assert.Equal(13m, row.Data.BaseCost);
    }

    [Fact]
    public void ApplyRow_UsesTextPropertyNumericAliasToUpdateBaseCost()
    {
        var row = new ResourceDto
        {
            Data = new ResourceMetadata
            {
                BaseCost = 600m,
                Cost = 650m
            },
            Formulas = ["basecost=P1"],
            Properties =
            [
                new ResourcePropertyBindDto
                {
                    Id = 55,
                    DisplayName = "P1",
                    DataType = DataType.Text,
                    TextDefault = "10"
                }
            ]
        };

        ResourceFormulaApplier.ApplyRow(row, new Dictionary<ParamName, decimal>());

        Assert.Equal(10m, row.Data.BaseCost);
    }

    [Fact]
    public void ApplyRow_AssignsCalculatedValueBackToPropertyTarget()
    {
        var row = new ResourceDto
        {
            Data = new ResourceMetadata
            {
                BaseCost = 12m,
                Cost = 5m
            },
            Formulas = ["P1=basecost+cost"],
            Properties =
            [
                new ResourcePropertyBindDto
                {
                    Id = 55,
                    DisplayName = "P1",
                    DataType = DataType.Number,
                    NumberDefault = 1m
                }
            ]
        };

        ResourceFormulaApplier.ApplyRow(row, new Dictionary<ParamName, decimal>());

        Assert.Equal(17m, row.Properties[0].NumberDefault);
    }

    [Fact]
    public void ApplyRow_AppliesMultipleFormulasInListOrder()
    {
        var row = new ResourceDto
        {
            Data = new ResourceMetadata
            {
                BaseCost = 600m,
                Cost = 0m
            },
            Formulas =
            [
                "cost=P1",
                "basecost=cost+5"
            ],
            Properties =
            [
                new ResourcePropertyBindDto
                {
                    Id = 55,
                    DisplayName = "P1",
                    DataType = DataType.Number,
                    NumberDefault = 10m
                }
            ]
        };

        ResourceFormulaApplier.ApplyRow(row, new Dictionary<ParamName, decimal>());

        Assert.Equal(10m, row.Data.Cost);
        Assert.Equal(15m, row.Data.BaseCost);
    }
}
