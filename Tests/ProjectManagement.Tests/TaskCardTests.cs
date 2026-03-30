using ProjectManagement.Client.Pages.Project.Storage.App;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using Xunit;

namespace ProjectManagement.Tests;

public class TaskCardTests
{
    [Fact]
    public void EnsureUncontrollableSummary_WhenTaskIsControllable_DoesNotCopyResultResourcesIntoBaseResources()
    {
        var task = new ProjectTaskDto
        {
            Id = 15,
            Uncontrollable = false,
            BaseResources = [CreateResource(1, "Base resource")],
            ResultResources = [CreateResource(2, "Derived resource")]
        };
        var component = new TaskCard();
        SetTask(component, task);

        InvokeEnsureUncontrollableSummary(component);
        InvokeEnsureUncontrollableSummary(component);

        Assert.Single(task.BaseResources);
        Assert.Equal(1, task.BaseResources[0].Id);
        Assert.Single(task.ResultResources);
        Assert.Equal(2, task.ResultResources[0].Id);
        Assert.Equal(2, task.Resources.Count);
    }

    private static void InvokeEnsureUncontrollableSummary(TaskCard component)
    {
        var method = typeof(TaskCard).GetMethod("EnsureUncontrollableSummary", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        method.Invoke(component, null);
    }

    private static void SetTask(TaskCard component, ProjectTaskDto task)
    {
        var property = typeof(TaskCard).GetProperty(nameof(TaskCard.Task));
        Assert.NotNull(property);

        property.SetValue(component, task);
    }

    private static ResourceDto CreateResource(int id, string name) => new()
    {
        Id = id,
        Name = name,
        Data = new ResourceMetadata
        {
            Unit = "m",
            Cost = 10m,
            Quantity = 2m
        }
    };
}
