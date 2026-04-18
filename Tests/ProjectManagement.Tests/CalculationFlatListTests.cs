using ProjectManagement.Client.Shared.Calculation;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Client.Shared.ViewModel;
using ProjectManagement.Shared.Base.Calculation;
using Xunit;

namespace ProjectManagement.Tests;

public class CalculationFlatListTests
{
    [Fact]
    public void BuildFlatList_WhenResourcesAreHidden_ReturnsOnlyTaskRows()
    {
        var calculation = CreateCalculation();
        calculation.ShowTasks = true;
        calculation.ShowResources = false;

        var flat = calculation.BuildFlatList();

        Assert.Equal(2, flat.Count);
        Assert.All(flat, item => Assert.True(item.IsTask));
        Assert.Equal(0, flat[0].Depth);
        Assert.Equal(1, flat[1].Depth);
    }

    [Fact]
    public void BuildFlatList_WhenTasksAreHidden_ReturnsOnlyVisibleResourceRows()
    {
        var calculation = CreateCalculation();
        calculation.ShowTasks = false;
        calculation.ShowResources = true;

        var flat = calculation.BuildFlatList();

        Assert.Equal(2, flat.Count);
        Assert.All(flat, item => Assert.True(item.IsResource));
        Assert.Equal(10, flat[0].Resource!.Id);
        Assert.Equal(20, flat[1].Resource!.Id);
        Assert.Equal(1, flat[0].Depth);
        Assert.Equal(2, flat[1].Depth);
    }

    [Fact]
    public void BuildFlatList_WhenOnlyActiveEnabled_HidesResourcesOfInactiveTaskBranches()
    {
        var calculation = CreateCalculation();
        calculation.Tasks[0].Metadata.IsActive = false;
        calculation.ShowTasks = false;
        calculation.ShowResources = true;
        calculation.OnlyActive = true;

        var flat = calculation.BuildFlatList();

        Assert.Empty(flat);
    }

    [Fact]
    public void BuildFlatList_WhenOnlyCodeTextTasksAreHidden_KeepsVisibleDescendants()
    {
        var calculation = CreateCalculation();
        calculation.Tasks[0].Metadata.Type = TaskType.CodeName;
        calculation.Tasks[0].Resources.Clear();
        calculation.RebuildHierarchyAndIndexes();
        calculation.ShowOnlyCodeTextTasks = false;

        var flat = calculation.BuildFlatList();

        Assert.Collection(
            flat,
            item =>
            {
                Assert.True(item.IsTask);
                Assert.Equal(2, item.Task!.Id);
            },
            item =>
            {
                Assert.True(item.IsResource);
                Assert.Equal(20, item.Resource!.Id);
            });
    }

    [Fact]
    public void BuildFlatList_WhenParentTaskInactive_TracksParentActiveForChildRows()
    {
        var calculation = CreateCalculation();
        calculation.Tasks[0].Metadata.IsActive = false;
        calculation.OnlyActive = false;
        calculation.ShowTasks = true;
        calculation.ShowResources = true;

        var flat = calculation.BuildFlatList();

        Assert.Collection(
            flat,
            item =>
            {
                Assert.True(item.IsTask);
                Assert.Equal(1, item.Task!.Id);
                Assert.True(item.ParentActive);
            },
            item =>
            {
                Assert.True(item.IsResource);
                Assert.Equal(10, item.Resource!.Id);
                Assert.False(item.ParentActive);
            },
            item =>
            {
                Assert.True(item.IsTask);
                Assert.Equal(2, item.Task!.Id);
                Assert.False(item.ParentActive);
            },
            item =>
            {
                Assert.True(item.IsResource);
                Assert.Equal(20, item.Resource!.Id);
                Assert.False(item.ParentActive);
            });
    }

    [Fact]
    public void TryToggleTaskCollapse_UpdatesExistingFlatListIncrementally()
    {
        var calculation = CreateCalculation();
        var rootTask = calculation.Tasks[0];

        calculation.AllFlatItems = calculation.BuildFlatList();
        calculation.FlatListDirty = false;

        Assert.Collection(
            calculation.AllFlatItems,
            item => Assert.Equal(1, item.Task!.Id),
            item => Assert.Equal(10, item.Resource!.Id),
            item => Assert.Equal(2, item.Task!.Id),
            item => Assert.Equal(20, item.Resource!.Id));

        var collapsed = calculation.TryToggleTaskCollapse(rootTask);
        calculation.AllFlatItems = calculation.BuildFlatList();
        calculation.FlatListDirty = false;

        Assert.True(collapsed);
        Assert.Single(calculation.AllFlatItems!);
        Assert.Equal(1, calculation.AllFlatItems[0].Task!.Id);

        var expanded = calculation.TryToggleTaskCollapse(rootTask);
        calculation.AllFlatItems = calculation.BuildFlatList();
        calculation.FlatListDirty = false;

        Assert.True(expanded);
        Assert.Collection(
            calculation.AllFlatItems!,
            item => Assert.Equal(1, item.Task!.Id),
            item => Assert.Equal(10, item.Resource!.Id),
            item => Assert.Equal(2, item.Task!.Id),
            item => Assert.Equal(20, item.Resource!.Id));
    }

    [Fact]
    public void Filter_WhenChildTaskMatches_AlsoKeepsParentVisible()
    {
        var calculation = CreateCalculation();
        var filter = new FilterVM
        {
            Name = ["child-task"],
            NameFilterType = FilterType.equals
        };

        CalculationStaticFun.Filter(filter, calculation.Tasks);

        Assert.True(calculation.Tasks[0].Ui.FilterVisible);
        Assert.True(calculation.Tasks[1].Ui.FilterVisible);
        Assert.False(calculation.Tasks[0].Resources[0].Ui.FilterVisible);
        Assert.False(calculation.Tasks[1].Resources[0].Ui.FilterVisible);
    }

    [Fact]
    public void Filter_WhenResourceMatches_AlsoKeepsOwningTaskTreeVisible()
    {
        var calculation = CreateCalculation();
        var filter = new FilterVM
        {
            Account = ["ACC-20"],
            AccountFilterType = FilterType.equals
        };

        CalculationStaticFun.Filter(filter, calculation.Tasks);

        Assert.True(calculation.Tasks[0].Ui.FilterVisible);
        Assert.True(calculation.Tasks[1].Ui.FilterVisible);
        Assert.False(calculation.Tasks[0].Resources[0].Ui.FilterVisible);
        Assert.True(calculation.Tasks[1].Resources[0].Ui.FilterVisible);
    }

    [Fact]
    public void ResourceOfferSelectionCache_TracksRemovalOfSelectedOffer()
    {
        var resource = new ResourceListMVVM
        {
            Id = 10,
            OfferId = 2,
            Offers =
            [
                new ListOfferMVVM { Id = 1 },
                new ListOfferMVVM { Id = 2 }
            ]
        };

        resource.SyncOfferSelection();
        Assert.True(resource.HasOfferSelected());

        resource.RemoveOffer(2);

        Assert.False(resource.HasOfferSelected());
        Assert.Null(resource.OfferId);
        Assert.Single(resource.Offers);
    }

    private static CalculationMVVM CreateCalculation()
    {
        var rootTask = new TaskListMVVM
        {
            Id = 1,
            Name = "root-task",
            Resources =
            [
                new ResourceListMVVM { Id = 10, TaskId = 1, Name = "root-resource", AccountCode = "ACC-10" }
            ]
        };
        rootTask.Metadata.IsActive = true;

        var childTask = new TaskListMVVM
        {
            Id = 2,
            TaskId = 1,
            Name = "child-task",
            Resources =
            [
                new ResourceListMVVM { Id = 20, TaskId = 2, Name = "child-resource", AccountCode = "ACC-20" }
            ]
        };
        childTask.Metadata.IsActive = true;

        var calculation = new CalculationMVVM
        {
            Tasks = [rootTask, childTask]
        };

        calculation.RebuildHierarchyAndIndexes();
        return calculation;
    }
}
