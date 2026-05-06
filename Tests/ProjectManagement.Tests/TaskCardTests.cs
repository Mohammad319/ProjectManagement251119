using ProjectManagement.Client.Pages.Project.Storage.App;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using Xunit;

namespace ProjectManagement.Tests;

public class TaskCardTests
{
    [Fact]
    public void SelectedInputUnitCode_WhenNewUnitIsEmpty_FallsBackToTaskUnit()
    {
        var task = new ProjectTaskDto
        {
            UnitCode = "m3",
            NewUnitCode = string.Empty
        };
        var component = new TaskCard();
        SetTask(component, task);

        var selectedUnit = GetSelectedInputUnitCode(component);

        Assert.Equal("m3", selectedUnit);
    }

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
        var method = typeof(TaskCard).GetMethod("EnsureUncontrollableSummary", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        method.Invoke(component, null);
    }

    private static string GetSelectedInputUnitCode(TaskCard component)
    {
        var property = typeof(TaskCard).GetProperty("SelectedInputUnitCode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(property);

        return Assert.IsType<string>(property.GetValue(component));
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
        Unit = "m",
        Quantity = 2m,
        Data = new ResourceMetadata
        {
            Cost = 10m
        }
    };
}
