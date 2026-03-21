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
        Assert.Equal(0.4m, resource.Cost);
        Assert.Equal(0.4m, resource.NetCostQ);
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
        decimal changeFactor2)
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
                Times = [.. times.Select((item, index) => new ResourceTime
                {
                    Name = $"T{index + 1}",
                    Quantity = item.Quantity,
                    Cost = item.Cost
                })]
            }
        };

        return resource;
    }
}
