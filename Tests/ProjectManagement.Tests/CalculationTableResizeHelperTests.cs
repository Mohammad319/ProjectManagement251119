using ProjectManagement.Client.Shared.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation.Template;
using Xunit;

namespace ProjectManagement.Tests;

public class CalculationTableResizeHelperTests
{
    [Fact]
    public void TryParseResizePayload_ClampsWidthToMinimum()
    {
        var result = CalculationTableResizeHelper.TryParseResizePayload("3||5", out int headerIndex, out int width);

        Assert.True(result);
        Assert.Equal(3, headerIndex);
        Assert.Equal(PMValuesConst.MinWidthCol, width);
    }

    [Fact]
    public void TryResolveColumnId_UsesVisibleHeaderOrder()
    {
        IReadOnlyList<NetColumnId> visibleColumns =
        [
            NetColumnId.Name,
            NetColumnId.Status
        ];

        var resolved = CalculationTableResizeHelper.TryResolveColumnId(3, visibleColumns, out var columnId);

        Assert.True(resolved);
        Assert.Equal(NetColumnId.Status, columnId);
    }
}
