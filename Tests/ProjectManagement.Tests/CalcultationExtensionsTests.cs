using ProjectManagement.Client.Extensions.CalcultationItemsOperation;
using ProjectManagement.Client.Services.Calculation.CalculationItems;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Enums;
using Xunit;

namespace ProjectManagement.Tests;

public class CalcultationExtensionsTests
{
    [Fact]
    public void ExecuteCalculation_UsesParametersAndTimesToDeriveResourceQuantityAndCost()
    {
        var resource = CreateResource(
            parameters: [2m, 3m],
            times:
            [
                (2m, 60m),
                (1m, 120m)
            ],
            changeFactor2: 4m);

        var calc = CreateCalculation(resource, 10m);

        calc.ExecuteCalculation();

        Assert.Equal(6m, resource.Data.ChangeFactor1);
        Assert.Equal(240m, resource.Quantity);
        Assert.Equal(1m, resource.Cost);
        Assert.Equal(1m, resource.NetCostQ);
        Assert.Equal(240m, resource.GetComputedNetCostTotaly());
    }

    [Fact]
    public void ExecuteCalculation_RecalculatesComputedCachesAfterParameterValuesChange()
    {
        var resource = CreateResource(
            parameters: [2m, 3m],
            times:
            [
                (2m, 60m),
                (1m, 120m)
            ],
            changeFactor2: 4m);

        var calc = CreateCalculation(resource, 10m);

        calc.ExecuteCalculation();
        Assert.Equal(1m, resource.NetCostQ);

        resource.Data.Parameters[0].Value = 5m;
        calc.ExecuteCalculation();

        Assert.Equal(15m, resource.Data.ChangeFactor1);
        Assert.Equal(600m, resource.Quantity);
        Assert.Equal(1m, resource.Cost);
        Assert.Equal(1m, resource.NetCostQ);
    }

    [Fact]
    public void ExecuteCalculation_RecalculatesTimeQuantitiesFromPercentages()
    {
        var resource = CreateResource(parameters: [], times: [], changeFactor2: 4m);
        resource.Data.Times =
        [
            new ResourceTime { Name = "Morning", Percentage = 75m, Cost = 60m },
            new ResourceTime { Name = "Evening", Percentage = 25m, Cost = 120m }
        ];

        var calc = CreateCalculation(resource, 10m);

        calc.ExecuteCalculation();

        Assert.Equal(40m, resource.Quantity);
        Assert.Equal(30m, resource.Data.Times[0].Quantity);
        Assert.Equal(10m, resource.Data.Times[1].Quantity);
        Assert.Equal(75m, resource.Cost);
    }

    [Fact]
    public void ExecuteCalculation_IncludesAddOnsInEffectiveCostAndBaseCost()
    {
        var resource = CreateResource(
            parameters: [],
            times: [],
            changeFactor2: 1m,
            addOns:
            [
                (4m, 3m, 1m),
                (2m, 2m, 0.5m)
            ]);

        resource.Data.Cost = 5m;
        resource.Data.BaseCost = 2m;

        var calc = CreateCalculation(resource, 10m);

        calc.ExecuteCalculation();

        Assert.Equal(21m, resource.GetComputedCost());
        Assert.Equal(3.5m, resource.GetComputedBaseCost());
        Assert.Equal(213.5m, resource.GetComputedNetCostTotaly());
    }

    [Fact]
    public void ResourceService_AffectsCalculation_DetectsParameterDrivenChange()
    {
        var oldResource = CreateResource(parameters: [2m, 3m], times: [], changeFactor2: 4m);
        oldResource.Data.Quantity = 240m;

        var newResource = CreateResource(parameters: [2m, 4m], times: [], changeFactor2: 4m);
        newResource.Data.Quantity = 240m;

        Assert.True(ResourceService.AffectsCalculation(oldResource, newResource));
    }

    [Fact]
    public void ResourceService_AffectsCalculation_DetectsTimeDrivenCostChange()
    {
        var oldResource = CreateResource(
            parameters: [],
            times:
            [
                (2m, 60m),
                (1m, 120m)
            ],
            changeFactor2: 1m);
        oldResource.Data.Quantity = 240m;

        var newResource = CreateResource(
            parameters: [],
            times:
            [
                (2m, 60m),
                (1m, 240m)
            ],
            changeFactor2: 1m);
        newResource.Data.Quantity = 240m;

        Assert.True(ResourceService.AffectsCalculation(oldResource, newResource));
    }

    [Fact]
    public void ResourceService_AffectsCalculation_DetectsAddOnDrivenCostChange()
    {
        var oldResource = CreateResource(
            parameters: [],
            times: [],
            changeFactor2: 1m,
            addOns:
            [
                (4m, 3m, 1m)
            ]);
        oldResource.Data.Quantity = 10m;
        oldResource.Data.Cost = 5m;
        oldResource.Data.BaseCost = 2m;

        var newResource = CreateResource(
            parameters: [],
            times: [],
            changeFactor2: 1m,
            addOns:
            [
                (4m, 4m, 1m)
            ]);
        newResource.Data.Quantity = 10m;
        newResource.Data.Cost = 5m;
        newResource.Data.BaseCost = 2m;

        Assert.True(ResourceService.AffectsCalculation(oldResource, newResource));
    }

    private static CalculationMVVM CreateCalculation(ResourceListMVVM resource, decimal rootQuantity)
    {
        var task = new TaskListMVVM
        {
            Id = 1,
            Name = "Root Task",
            Metadata = new TaskMetadata
            {
                QuantityParam = "rootQ",
                Type = TaskType.Task
            },
            Resources = [resource],
            Tasks = []
        };

        var calc = new CalculationMVVM
        {
            Tasks = [task],
            QuanityList =
            [
                new QuanityListDTO
                {
                    Name = "rootQ",
                    Quantity = rootQuantity
                }
            ]
        };

        calc.RebuildHierarchyAndIndexes();
        return calc;
    }

    private static ResourceListMVVM CreateResource(
        IReadOnlyList<decimal> parameters,
        IReadOnlyList<(decimal Quantity, decimal Cost)> times,
        decimal changeFactor2,
        IReadOnlyList<(decimal Quantity, decimal Cost, decimal BaseCost)>? addOns = null)
    {
        var resource = new ResourceListMVVM
        {
            Id = 10,
            TaskId = 1,
            Name = "Resource",
            ResType = ResourceTypesEnum.Materials,
            Data = new ResourceMetadata
            {
                ChangeFactor1 = 1m,
                ChangeFactor2 = changeFactor2,
                Cost = 10m,
                Parameters = [.. parameters.Select((value, index) => new ResourceParameter
                {
                    Name = $"P{index + 1}",
                    Unit = "u",
                    Value = value
                })],
                AddOns = addOns is null
                    ? []
                    : [.. addOns.Select((item, index) => new ResourceAddon
                    {
                        Name = $"A{index + 1}",
                        Unit = "u",
                        Factor = item.Quantity,
                        Cost = item.Cost,
                        BaseCost = item.BaseCost
                    })],
                Times = [.. times.Select((item, index) => new ResourceTime
                {
                    Name = $"T{index + 1}",
                    Cost = item.Cost
                }.SetResolvedQuantity(item.Quantity))],
            }
        };

        return resource;
    }
}
