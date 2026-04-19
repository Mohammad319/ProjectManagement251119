using Microsoft.AspNetCore.Components;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;

public partial class CalculationDataRow
{
    [Parameter] public string RowStyle { get; set; } = string.Empty;
    [Parameter] public int Left { get; set; }
    [Parameter] public bool CanToggle { get; set; }
    [Parameter] public bool IsExpanded { get; set; }
    [Parameter] public bool ShowHierarchyPlaceholder { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public EventCallback OnContextMenu { get; set; }
    [Parameter] public EventCallback OnToggle { get; set; }
    [Parameter] public bool ShowActiveToggle { get; set; }
    [Parameter] public bool IsItemActive { get; set; } = true;
    [Parameter] public EventCallback OnToggleActive { get; set; }
    [Parameter] public RenderFragment? Cells { get; set; }
    [Parameter] public IReadOnlyList<string>? Notes { get; set; }
    [Parameter] public int ValueColumns { get; set; }
}
