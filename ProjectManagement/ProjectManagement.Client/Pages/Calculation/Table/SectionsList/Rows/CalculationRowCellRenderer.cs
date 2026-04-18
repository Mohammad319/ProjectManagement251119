using Microsoft.AspNetCore.Components.Rendering;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;

public static class CalculationRowCellRenderer
{
    public static void RenderTaskCells(
        RenderTreeBuilder builder,
        IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> columns,
        TaskListMVVM task,
        int namePaddingPx = 0) =>
        RenderCells(builder, columns.Count, i =>
        {
            if (namePaddingPx > 0 && columns[i].Id == NetColumnId.Name)
                TableRenderHelpers.RenderWithTitleIndented(builder, task.Name, namePaddingPx);
            else
                columns[i].TaskRender(builder, task);
        });

    public static void RenderResourceCells(
        RenderTreeBuilder builder,
        IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> columns,
        ResourceListMVVM resource,
        int namePaddingPx = 0) =>
        RenderCells(builder, columns.Count, i =>
        {
            if (namePaddingPx > 0 && columns[i].Id == NetColumnId.Name)
                TableRenderHelpers.RenderWithTitleIndented(builder, resource.Name, namePaddingPx);
            else
                columns[i].ResRender(builder, resource);
        });

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
