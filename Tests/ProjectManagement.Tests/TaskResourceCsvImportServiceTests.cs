using System.Reflection;
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
}
