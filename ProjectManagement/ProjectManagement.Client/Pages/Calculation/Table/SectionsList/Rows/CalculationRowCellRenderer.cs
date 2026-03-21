using Microsoft.AspNetCore.Components.Rendering;
using ProjectManagement.Client.Shared.MVVM.Calculation;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;

public static class CalculationRowCellRenderer
{
    public static void RenderTaskCells(
        RenderTreeBuilder builder,
        IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> columns,
        TaskListMVVM task) =>
        RenderCells(builder, columns.Count, i => columns[i].TaskRender(builder, task));

    public static void RenderResourceCells(
        RenderTreeBuilder builder,
        IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> columns,
        ResourceListMVVM resource) =>
        RenderCells(builder, columns.Count, i => columns[i].ResRender(builder, resource));

    private static void RenderCells(RenderTreeBuilder builder, int count, Action<int> renderCell)
    {
        var seq = 0;
        for (int i = 0; i < count; i++)
        {
            builder.OpenRegion(seq++);
            renderCell(i);
            builder.CloseRegion();
        }
    }
}
