using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;
using ProjectManagement.Shared.Enums;
using Xunit;

namespace ProjectManagement.Tests;

public class TasksUserComputationServiceWasmTests
{
    [Fact]
    public void CalcQuantityResource_UsesParametersAndTimesToDeriveValues()
    {
        var service = new TasksUserComputationServiceWasm();
        var resource = CreateResource(parameters: [2m, 3m], times: [(2m, 60m), (1m, 120m)], changeFactor2: 4m);

        service.CalcQuantityResource([resource], 10m);

        Assert.Equal(6m, resource.Data.ChangeFactor1);
        Assert.Equal(240m, resource.Data.Quantity);
        Assert.Equal(1m, resource.Data.Cost);
    }

    [Fact]
    public void BuildFinalRows_PreservesNestedResourceMetadataForDerivedCalculation()
    {
        var service = new TasksUserComputationServiceWasm();
        var sourceResource = CreateResource(parameters: [2m, 3m], times: [(2m, 60m), (1m, 120m)], changeFactor2: 4m);
        var task = new ProjectTaskDto
        {
            Id = 1,
            Quantity = 10m,
            Conditions =
            [
                new TaskConditionDto
                {
                    ConditionResourceAssignments =
                    [
                        new ResourceAssignmentDto
                        {
                            Resource = sourceResource
                        }
                    ]
                }
            ]
        };

        var result = service.BuildFinalRows(task);

        Assert.True(result);

        var resource = Assert.Single(task.ResultResources);
        Assert.Equal(2, resource.Data.Parameters.Count);
        Assert.Equal(2, resource.Data.Times.Count);
        Assert.Equal(6m, resource.Data.ChangeFactor1);
        Assert.Equal(240m, resource.Data.Quantity);
        Assert.Equal(1m, resource.Data.Cost);
    }

    [Fact]
    public void BuildFinalRows_DeduplicatesDerivedResourcesWithSameIdAndMenu()
    {
        var service = new TasksUserComputationServiceWasm();
        var sourceResource = CreateResource(parameters: [], times: [], changeFactor2: 1m);
        var task = new ProjectTaskDto
        {
            Id = 2,
            Quantity = 5m,
            Conditions =
            [
                new TaskConditionDto
                {
                    ConditionResourceAssignments =
                    [
                        new ResourceAssignmentDto
                        {
                            Resource = sourceResource
                        }
                    ]
                },
                new TaskConditionDto
                {
                    ConditionResourceAssignments =
                    [
                        new ResourceAssignmentDto
                        {
                            Resource = sourceResource
                        }
                    ]
                }
            ]
        };

        var result = service.BuildFinalRows(task);

        Assert.True(result);
        Assert.Single(task.ResultResources);
    }

    private static ResourceDto CreateResource(
        IReadOnlyList<decimal> parameters,
        IReadOnlyList<(decimal Quantity, decimal Cost)> times,
        decimal changeFactor2)
    {
        return new ResourceDto
        {
            Id = 10,
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
    }
}
