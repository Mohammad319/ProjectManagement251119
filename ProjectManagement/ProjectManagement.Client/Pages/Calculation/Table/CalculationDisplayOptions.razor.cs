using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;

namespace ProjectManagement.Client.Pages.Calculation.Table;

public partial class CalculationDisplayOptions : ComponentBase
{
    [Inject] private ICalculationTableCoordinator TableCoordinator { get; set; } = default!;
    [Inject] private CalculationService CalcService { get; set; } = default!;
    [Inject] private FolderState FolderState { get; set; } = default!;

    private bool IsOpen { get; set; }
    private CalculationMVVM Calc => FolderState.Calculation ?? throw new InvalidOperationException("CalculationDisplayOptions requires an active calculation.");

    private void ToggleDropdown() => IsOpen = !IsOpen;

    private void NotifyStructureRefresh() => TableCoordinator.NotifyStructureRefresh();
}
