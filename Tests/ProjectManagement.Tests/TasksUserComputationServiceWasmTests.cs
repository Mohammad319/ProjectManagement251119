using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper.ProjectAppStorage;
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
    public void CalcQuantityResource_RecalculatesTimeQuantitiesFromPercentages()
    {
        var service = new TasksUserComputationServiceWasm();
        var resource = CreateResource(parameters: [], times: [], changeFactor2: 4m);
        resource.Data.Times =
        [
            new ResourceTime { Name = "Morning", Percentage = 75m, Cost = 60m },
            new ResourceTime { Name = "Evening", Percentage = 25m, Cost = 120m }
        ];

        service.CalcQuantityResource([resource], 10m);

        Assert.Equal(40m, resource.Data.Quantity);
        Assert.Equal(30m, resource.Data.Times[0].Quantity);
        Assert.Equal(10m, resource.Data.Times[1].Quantity);
        Assert.Equal(75m, resource.Data.Cost);
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

    [Fact]
    public void BuildFinalRows_PreservesUserEditedDerivedResourceValuesAcrossRebuilds()
    {
        var service = new TasksUserComputationServiceWasm();
        var sourceResource = CreateResource(parameters: [], times: [], changeFactor2: 1m);
        sourceResource.Data.BaseCost = 10m;
        sourceResource.Data.Cost = 5m;
        sourceResource.Properties =
        [
            new ResourcePropertyBindDto
            {
                Id = 100,
                DisplayName = "P1",
                DataType = DataType.Number,
                NumberDefault = 3m
            }
        ];

        var task = new ProjectTaskDto
        {
            Id = 3,
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
                }
            ]
        };

        Assert.True(service.BuildFinalRows(task));

        var edited = Assert.Single(task.ResultResources);
        edited.IsAdded = true;
        edited.Name = "Edited Resource";
        edited.Data.ChangeFactor1 = 7m;
        edited.Data.ChangeFactor2 = 8m;
        edited.Data.BaseCost = 99m;
        edited.Data.Cost = 123m;
        edited.Data.CO2 = 42d;
        edited.Properties[0].NumberDefault = 77m;

        Assert.True(service.BuildFinalRows(task));

        var rebuilt = Assert.Single(task.ResultResources);
        Assert.True(rebuilt.IsAdded);
        Assert.Equal("Edited Resource", rebuilt.Name);
        Assert.Equal(7m, rebuilt.Data.ChangeFactor1);
        Assert.Equal(8m, rebuilt.Data.ChangeFactor2);
        Assert.Equal(99m, rebuilt.Data.BaseCost);
        Assert.Equal(123m, rebuilt.Data.Cost);
        Assert.Equal(42d, rebuilt.Data.CO2);
        Assert.Equal(77m, rebuilt.Properties[0].NumberDefault);
    }

    [Fact]
    public void EvaluateVariables_UsesPriceProductionVariable()
    {
        var service = new TasksUserComputationServiceWasm();
        var task = new ProjectTaskDto
        {
            Quantity = 4m,
            PriceProduction = 40m,
            BaseResources =
            [
                new ResourceDto
                {
                    Id = 50,
                    Name = "Base",
                    ResType = ResourceTypesEnum.Materials,
                    Data = new ResourceMetadata
                    {
                        Cost = 25m,
                        Quantity = 4m
                    }
                }
            ]
        };

        var condition = new TaskConditionDto
        {
            VariableRequirements =
            [
                new ConditionVariableRequirementDto
                {
                    VariableName = "priceproduction",
                    MinAllowedValue = 39m,
                    MaxAllowedValue = 41m,
                    SetKey = 1
                }
            ]
        };

        Assert.True(service.EvaluateVariables(task, condition));
    }

    [Fact]
    public void RefreshResources_RecalculatesQuantityAndCostAfterFormulaChangesChangeFactor()
    {
        var service = new TasksUserComputationServiceWasm();
        var resource = new ResourceDto
        {
            Id = 11,
            Name = "Formula Resource",
            ResType = ResourceTypesEnum.Materials,
            Formulas = ["ch1=P1"],
            Properties =
            [
                new ResourcePropertyBindDto
                {
                    Id = 7,
                    DisplayName = "P1",
                    DataType = DataType.Number,
                    NumberDefault = 2m
                }
            ],
            CostRole =
            [
                new RoleDTO
                {
                    Min = 50m,
                    Max = 100m,
                    Value = 99m
                }
            ],
            Data = new ResourceMetadata
            {
                ChangeFactor1 = 1m,
                ChangeFactor2 = 3m,
                Cost = 10m
            }
        };

        service.RefreshResources([resource], 10m, new Dictionary<ParamName, decimal>());

        Assert.Equal(2m, resource.Data.ChangeFactor1);
        Assert.Equal(60m, resource.Data.Quantity);
        Assert.Equal(99m, resource.Data.Cost);
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
                    Cost = item.Cost
                }.SetResolvedQuantity(item.Quantity))]
            }
        };
    }
}
