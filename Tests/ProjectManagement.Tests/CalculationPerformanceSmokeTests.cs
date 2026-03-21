using System.Diagnostics;
using ProjectManagement.Client.Shared.Calculation;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.ViewModel;
using Xunit;
using Xunit.Abstractions;

namespace ProjectManagement.Tests;

[Collection("CalculationPerf")]
public class CalculationPerformanceSmokeTests(ITestOutputHelper output)
{
    private const int RootTaskCount = 100;
    private const int ChildrenPerRoot = 9;
    private const int ResourcesPerTask = 10;
    private const int TotalTaskCount = RootTaskCount * (ChildrenPerRoot + 1);
    private const int TotalResourceCount = TotalTaskCount * ResourcesPerTask;
    private const int TotalVisibleRows = TotalTaskCount + TotalResourceCount;
    private const int CollapsedBranchRowsRemoved = ResourcesPerTask + ChildrenPerRoot + (ChildrenPerRoot * ResourcesPerTask);

    private static readonly TimeSpan BuildFlatListBudget = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan FilterBudget = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ToggleCollapseBudget = TimeSpan.FromSeconds(2);

    [Fact]
    [Trait("Category", "PerformanceSmoke")]
    public void BuildFlatList_LargeCalculation_CompletesWithinBudget()
    {
        var scenario = CreateLargeCalculation();

        var sw = Stopwatch.StartNew();
        var flat = scenario.Calculation.BuildFlatList();
        sw.Stop();

        output.WriteLine($"BuildFlatList: {sw.ElapsedMilliseconds} ms for {TotalTaskCount} tasks and {TotalResourceCount} resources.");

        Assert.Equal(TotalVisibleRows, flat.Count);
        Assert.Equal(RootTaskCount, scenario.Calculation.RootTasks.Count);
        AssertWithinBudget(sw.Elapsed, BuildFlatListBudget);
    }

    [Fact]
    [Trait("Category", "PerformanceSmoke")]
    public void FilterAndBuildFlatList_LargeCalculation_CompletesWithinBudgetAndKeepsMatchingBranch()
    {
        var scenario = CreateLargeCalculation();
        var filter = new FilterVM
        {
            Account = ["ACC-TARGET"],
            AccountFilterType = FilterType.equals
        };

        var sw = Stopwatch.StartNew();
        CalculationStaticFun.Filter(filter, scenario.Calculation.Tasks);
        var flat = scenario.Calculation.BuildFlatList();
        sw.Stop();

        output.WriteLine($"Filter + BuildFlatList: {sw.ElapsedMilliseconds} ms for {TotalVisibleRows} logical rows.");

        Assert.Collection(
            flat,
            item => Assert.Equal(scenario.TargetRootTaskId, item.Task!.Id),
            item => Assert.Equal(scenario.TargetChildTaskId, item.Task!.Id),
            item => Assert.Equal(scenario.TargetResourceId, item.Resource!.Id));

        AssertWithinBudget(sw.Elapsed, FilterBudget);
    }

    [Fact]
    [Trait("Category", "PerformanceSmoke")]
    public void ToggleCollapse_LargeCalculation_CompletesWithinBudgetAndUpdatesVisibleRows()
    {
        var scenario = CreateLargeCalculation();
        var rootTask = scenario.Calculation.RootTasks[0];

        scenario.Calculation.AllFlatItems = scenario.Calculation.BuildFlatList();
        scenario.Calculation.FlatListDirty = false;

        var collapseSw = Stopwatch.StartNew();
        var collapsed = scenario.Calculation.TryToggleTaskCollapse(rootTask);
        var collapsedFlat = scenario.Calculation.BuildFlatList();
        collapseSw.Stop();

        output.WriteLine($"ToggleCollapse collapse: {collapseSw.ElapsedMilliseconds} ms.");

        Assert.True(collapsed);
        Assert.Equal(TotalVisibleRows - CollapsedBranchRowsRemoved, collapsedFlat.Count);
        AssertWithinBudget(collapseSw.Elapsed, ToggleCollapseBudget);

        scenario.Calculation.AllFlatItems = collapsedFlat;
        scenario.Calculation.FlatListDirty = false;

        var expandSw = Stopwatch.StartNew();
        var expanded = scenario.Calculation.TryToggleTaskCollapse(rootTask);
        var expandedFlat = scenario.Calculation.BuildFlatList();
        expandSw.Stop();

        output.WriteLine($"ToggleCollapse expand: {expandSw.ElapsedMilliseconds} ms.");

        Assert.True(expanded);
        Assert.Equal(TotalVisibleRows, expandedFlat.Count);
        AssertWithinBudget(expandSw.Elapsed, ToggleCollapseBudget);
    }

    private static void AssertWithinBudget(TimeSpan elapsed, TimeSpan budget)
    {
        Assert.InRange(elapsed, TimeSpan.Zero, budget);
    }

    private static LargeCalculationScenario CreateLargeCalculation()
    {
        int nextTaskId = 1;
        int nextResourceId = 1;

        int targetRootTaskId = 0;
        int targetChildTaskId = 0;
        int targetResourceId = 0;

        var tasks = new List<TaskListMVVM>(TotalTaskCount);

        for (int rootIndex = 0; rootIndex < RootTaskCount; rootIndex++)
        {
            int rootId = nextTaskId++;
            var rootTask = CreateTask(rootId, null, $"root-{rootIndex}", ref nextResourceId);
            tasks.Add(rootTask);

            if (rootIndex == 57)
                targetRootTaskId = rootId;

            for (int childIndex = 0; childIndex < ChildrenPerRoot; childIndex++)
            {
                int childId = nextTaskId++;
                var childTask = CreateTask(childId, rootId, $"child-{rootIndex}-{childIndex}", ref nextResourceId);

                if (rootIndex == 57 && childIndex == 4)
                {
                    targetChildTaskId = childId;
                    childTask.Resources[7].AccountCode = "ACC-TARGET";
                    targetResourceId = childTask.Resources[7].Id;
                }

                tasks.Add(childTask);
            }
        }

        var calculation = new CalculationMVVM
        {
            Tasks = tasks
        };

        calculation.RebuildHierarchyAndIndexes();

        return new LargeCalculationScenario(calculation, targetRootTaskId, targetChildTaskId, targetResourceId);
    }

    private static TaskListMVVM CreateTask(int id, int? parentId, string name, ref int nextResourceId)
    {
        var resources = new List<ResourceListMVVM>(ResourcesPerTask);
        for (int i = 0; i < ResourcesPerTask; i++)
        {
            int resourceId = nextResourceId++;
            resources.Add(new ResourceListMVVM
            {
                Id = resourceId,
                TaskId = id,
                Name = $"resource-{resourceId}",
                AccountCode = $"ACC-{resourceId}",
                Offers = []
            });
        }

        var task = new TaskListMVVM
        {
            Id = id,
            TaskId = parentId,
            Name = name,
            Resources = resources,
            Tasks = []
        };

        task.Metadata.IsActive = true;
        return task;
    }

    private readonly record struct LargeCalculationScenario(
        CalculationMVVM Calculation,
        int TargetRootTaskId,
        int TargetChildTaskId,
        int TargetResourceId);
}
