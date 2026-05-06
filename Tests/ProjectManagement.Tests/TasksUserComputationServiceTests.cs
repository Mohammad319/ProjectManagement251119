using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;
using ProjectManagement.Shared.Helper.ProjectAppStorage;
using Xunit;

namespace ProjectManagement.Tests;

public class TasksUserComputationServiceTests
{
    [Fact]
    public void ComputeOrThrow_ReturnsQuantity_WhenNormalizedUnitsMatch()
    {
        var service = new TasksUserComputationServiceWasm();

        var result = service.ComputeOrThrow(
            toUnit: Units.Piece,
            fromUnit: "st",
            quantity: 12.5m,
            parameters: new Dictionary<ParamName, decimal>());

        Assert.Equal(12.5m, result);
    }
}
