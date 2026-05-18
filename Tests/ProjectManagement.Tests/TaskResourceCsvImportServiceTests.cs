using System.Reflection;
using ProjectManagement.Shared.Enums;
using TaskResourceBlueprints.Services.Import;
using Xunit;

namespace ProjectManagement.Tests;

public class TaskResourceCsvImportServiceTests
{
    [Theory]
    [InlineData("5751", 5751)]
    [InlineData("5 751", 5751)]
    [InlineData("5\u00a0751", 5751)]
    [InlineData("5751,5", 5751.5)]
    public void ParseNullableDecimal_HandlesImportedQuantityFormats(string value, double expected)
    {
        var parsed = ParseNullableDecimal(value);

        Assert.Equal((decimal)expected, parsed);
    }

    [Fact]
    public void Get_UsesExpectedQuantityAndUnitFallbackColumns()
    {
        var row = new List<string>
        {
            "T",
            "task-1",
            "",
            "",
            "WSG",
            "Armering i fundament, huvudstr\u00e5k",
            "5751",
            "kg"
        };

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        Assert.Equal("5751", Get(row, map, "Quantity", fallbackIndex: 6));
        Assert.Equal("kg", Get(row, map, "Unit", fallbackIndex: 7));
    }

    [Fact]
    public void TaskImportKey_DistinguishesSameTaskCodeByStateValues()
    {
        var first = TaskImportKey(
            "KB-01",
            "K\u00e4rnborrning i betong, GC-v\u00e4g",
            [("Del", "GC-v\u00e4g"), ("Status", "Planerad")]);

        var second = TaskImportKey(
            "KB-01",
            "K\u00e4rnborrning i betong, GC-v\u00e4g",
            [("Del", "GC-v\u00e4g"), ("Status", "Utf\u00f6rd")]);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void TaskImportKey_IsStableWhenStateColumnsAreSwapped()
    {
        var first = TaskImportKey(
            "KB-01",
            "K\u00e4rnborrning i betong, GC-v\u00e4g",
            [("Del", "GC-v\u00e4g"), ("Status", "Planerad")]);

        var second = TaskImportKey(
            "KB-01",
            "K\u00e4rnborrning i betong, GC-v\u00e4g",
            [("Status", "Planerad"), ("Del", "GC-v\u00e4g")]);

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("Materials", ResourceTypesEnum.Materials)]
    [InlineData("MachinesAndEquipments", ResourceTypesEnum.MachinesAndEquipments)]
    [InlineData("Managers", ResourceTypesEnum.Managers)]
    [InlineData("ProjectOverheadCosts", ResourceTypesEnum.ProjectOverheadCosts)]
    [InlineData("Information", ResourceTypesEnum.Information)]
    [InlineData("Adjustment", ResourceTypesEnum.Adjustment)]
    public void ParseResourceType_AcceptsEnumNames(string value, ResourceTypesEnum expected)
    {
        var result = new TaskResourceCsvImportResult();

        Assert.Equal(expected, ParseResourceType(value, result));
        Assert.Empty(result.Issues);
    }

    private static decimal? ParseNullableDecimal(string value)
    {
        var method = typeof(TaskResourceCsvImportService).GetMethod(
            "ParseNullableDecimal",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (decimal?)method.Invoke(null, [value]);
    }

    private static string Get(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> map,
        string key,
        int? fallbackIndex)
    {
        var method = typeof(TaskResourceCsvImportService).GetMethod(
            "Get",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (string)method.Invoke(null, [row, map, key, fallbackIndex])!;
    }

    private static string TaskImportKey(
        string? code,
        string name,
        IEnumerable<(string Property, string Value)> statePairs)
    {
        var method = typeof(TaskResourceCsvImportService).GetMethod(
            "TaskImportKey",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (string)method.Invoke(null, [code, name, statePairs])!;
    }

    private static ResourceTypesEnum ParseResourceType(string value, TaskResourceCsvImportResult result)
    {
        var method = typeof(TaskResourceCsvImportService).GetMethod(
            "ParseResourceType",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (ResourceTypesEnum)method.Invoke(null, [value, 1, result])!;
    }
}
