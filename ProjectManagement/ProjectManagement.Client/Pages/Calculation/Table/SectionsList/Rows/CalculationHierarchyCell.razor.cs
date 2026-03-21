using Microsoft.AspNetCore.Components;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;

public partial class CalculationHierarchyCell
{
    [Parameter] public int IndentPx { get; set; }
    [Parameter] public bool CanToggle { get; set; }
    [Parameter] public bool IsExpanded { get; set; }
    [Parameter] public bool ShowPlaceholder { get; set; } = true;
    [Parameter] public EventCallback OnToggle { get; set; }

    private string IndentStyle => IndentPx > 0 ? $"padding-left:{IndentPx}px;" : string.Empty;

    private const string ToggleClass =
        "inline-flex items-center justify-center rounded-full " +
        "border border-slate-400 dark:border-slate-600 " +
        "bg-transparent dark:bg-transparent " +
        "w-5 h-5 cursor-pointer transition-all duration-200 ease-out " +
        "hover:opacity-70 hover:scale-110 hover:shadow-sm";

    private Task HandleToggle() => OnToggle.InvokeAsync();
}
