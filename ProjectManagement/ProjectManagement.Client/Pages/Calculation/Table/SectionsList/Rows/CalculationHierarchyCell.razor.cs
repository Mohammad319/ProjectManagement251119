using Microsoft.AspNetCore.Components;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;

public partial class CalculationHierarchyCell
{
    private const int StepPx = 16;

    [Parameter] public int IndentPx { get; set; }
    [Parameter] public bool CanToggle { get; set; }
    [Parameter] public bool IsExpanded { get; set; }
    [Parameter] public bool ShowPlaceholder { get; set; } = true;
    [Parameter] public EventCallback OnToggle { get; set; }

    private int Depth => StepPx > 0 ? IndentPx / StepPx : 0;

    private string IndentStyle => IndentPx > 0 ? $"padding-left:{IndentPx}px;" : string.Empty;

    private int GuideLeftPx(int guideIndex) => guideIndex * StepPx + StepPx / 2;

    private const string ToggleClass =
        "relative z-[1] inline-flex h-4 w-4 shrink-0 cursor-pointer items-center justify-center " +
        "rounded-sm border border-slate-300 bg-white text-[10px] font-semibold leading-none text-slate-700 " +
        "shadow-sm transition " +
        "hover:border-blue-500 hover:bg-blue-50 hover:text-blue-700 " +
        "dark:border-slate-600 dark:bg-slate-800 dark:text-slate-200 " +
        "dark:hover:border-blue-400 dark:hover:bg-blue-950";

    private Task HandleToggle() => OnToggle.InvokeAsync();
}
