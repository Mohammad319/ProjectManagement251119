using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;

public partial class TaskRowComponent : CalculationSelectableRowComponentBase
{
    private readonly RenderFragment _taskCells;

    public TaskRowComponent() => _taskCells = RenderTaskCells;

    [Parameter] public EventCallback<TaskListMVVM> OnCollapseToggle { get; set; }
    [Parameter] public TaskListMVVM Task { get; set; } = default!;
    [Parameter] public bool ActiveParent { get; set; }
    [Parameter] public string Color { get; set; } = TemplateConstBase.Task;
    [Parameter] public int Left { get; set; }
    [Parameter] public IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> Colmuns { get; set; } = Array.Empty<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>>();

    protected override CalculationItemType SelectionItemType => CalculationItemType.task;
    protected override int SelectionItemId => Task.Id;
    protected override decimal? SelectionQuantity => Task.Metadata?.Quantity;

    private string RowStyle => Task.Style(Color, ActiveParent, IsRowSelected);
    private bool HasDescendants => (Task.Resources?.Count ?? 0) > 0 || (Task.Tasks?.Count ?? 0) > 0;
    private bool HasConversionParameters => Task?.Metadata?.ConversionParameters?.Count > 0;

    private void RowClick()
    {
        SelectCurrentItem();
    }

    private System.Threading.Tasks.Task RowContext() => Task.Ui.ContextClick?.Invoke() ?? System.Threading.Tasks.Task.CompletedTask;
    private System.Threading.Tasks.Task ToggleCollapse() => OnCollapseToggle.InvokeAsync(Task);

    private void RenderTaskCells(RenderTreeBuilder builder)
        => CalculationRowCellRenderer.RenderTaskCells(builder, Colmuns, Task);
}
