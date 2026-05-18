using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Services.Training;
using Xunit;

namespace ProjectManagement.Tests;

public class TaskResourceTrainingDatasetServiceTests
{
    [Fact]
    public void CreateExample_BuildsPositiveTrainingFeatures()
    {
        var task = new TaskDefinition
        {
            Id = 10,
            Name = "Schakt för ledning",
            Code = "CBB.311",
            UnitCode = "m3",
            Quantity = 12m,
            UsageCount = 7
        };
        task.RefreshNormalizedTextSv();
        var resource = new ResourceDefinition
        {
            Id = 20,
            Name = "Grävmaskin för schakt",
            Unit = "m3",
            Quantity = 10m
        };

        var example = TaskResourceTrainingDatasetService.CreateExample(task, resource, 1.5m, label: true);

        Assert.True(example.Label);
        Assert.Equal(10, example.TaskId);
        Assert.Equal(20, example.ResourceId);
        Assert.Equal(1.5m, example.LinkQuantity);
        Assert.True(example.TextSimilarity > 0);
        Assert.True(example.NameSimilarity > 0);
        Assert.True(example.HasQuantitySimilarity);
        Assert.True(example.HasSameUnit);
        Assert.True(example.HasCompatibleUnit);
        Assert.Equal(7, example.TaskUsageCount);
    }

    [Fact]
    public void CreateExample_MarksUnrelatedNegativeExample()
    {
        var task = new TaskDefinition
        {
            Id = 10,
            Name = "Målning av vägg",
            UnitCode = "m2",
            Quantity = 30m
        };
        task.RefreshNormalizedTextSv();
        var resource = new ResourceDefinition
        {
            Id = 21,
            Name = "Lastbilstransport",
            Unit = "h",
            Quantity = 2m
        };

        var example = TaskResourceTrainingDatasetService.CreateExample(task, resource, 0m, label: false);

        Assert.False(example.Label);
        Assert.False(example.HasSameUnit);
        Assert.False(example.HasCompatibleUnit);
        Assert.True(example.QuantitySimilarity < 0.2);
    }
}
