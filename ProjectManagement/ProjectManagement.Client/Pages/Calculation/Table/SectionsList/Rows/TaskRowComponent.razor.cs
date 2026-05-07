using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;

public partial class TaskRowComponent : CalculationSelectableRowComponentBase
{
    private readonly RenderFragment _taskCells;

    public TaskRowComponent() => _taskCells = RenderTaskCells;

    [Parameter] public EventCallback<TaskListMVVM> OnCollapseToggle { get; set; }
    [Parameter] public TaskListMVVM Task { get; set; } = default!;
    [Parameter] public bool ActiveParent { get; set; }
    [Parameter] public bool IsItemActive { get; set; } = true;
    [Parameter] public bool ShowActiveToggle { get; set; }
    [Parameter] public EventCallback OnToggleActive { get; set; }
    [Parameter] public NetColor? Colors { get; set; }
    [Parameter] public int Left { get; set; }
    [Parameter] public IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> Colmuns { get; set; } = Array.Empty<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>>();

    protected override CalculationItemType SelectionItemType => CalculationItemType.task;
    protected override int SelectionItemId => Task.Id;
    protected override decimal? SelectionQuantity => Task.Quantity;

    private string ResolvedColor => Task.Type == TaskType.CodeName
        ? (Colors?.TaskCodeName ?? TemplateConstBase.TaskCodeName)
        : (Colors?.Task ?? TemplateConstBase.Task);

    private string InactiveTextColor => Colors?.InactiveText ?? TemplateConstBase.InactiveText;
    private string RowStyle => Task.Style(ResolvedColor, InactiveTextColor, ActiveParent, IsRowSelected);
    private bool HasDescendants => (Task.Resources?.Count ?? 0) > 0 || (Task.Tasks?.Count ?? 0) > 0;
    private bool HasConversionParameters => Task?.Metadata?.ConversionParameters?.Count > 0;
    private bool CanToggle => HasDescendants || HasConversionParameters;
    private bool CanShowConversionParameters => HasConversionParameters && Task.Ui.CollSpan;

    private void RowClick(MouseEventArgs e)
    {
        SelectCurrentItem(e);
    }

    private System.Threading.Tasks.Task RowContext() => Task.Ui.ContextClick?.Invoke() ?? System.Threading.Tasks.Task.CompletedTask;
    private System.Threading.Tasks.Task ToggleCollapse() => OnCollapseToggle.InvokeAsync(Task);

    private void RenderTaskCells(RenderTreeBuilder builder)
        => CalculationRowCellRenderer.RenderTaskCells(builder, Colmuns, Task, Left, ShowActiveToggle, ToggleActive, ActiveParent);

    private Task ToggleActive() => OnToggleActive.InvokeAsync();
}
