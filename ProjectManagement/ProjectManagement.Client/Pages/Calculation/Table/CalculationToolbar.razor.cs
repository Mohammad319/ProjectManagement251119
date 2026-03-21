using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles;

namespace ProjectManagement.Client.Pages.Calculation.Table;

public partial class CalculationToolbar : ComponentBase, IDisposable
{
    [Inject] private ICalculationTableCoordinator TableCoordinator { get; set; } = default!;
    [Inject] private IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;
    [Inject] private CalculationService CalcService { get; set; } = default!;
    [Inject] private CalculationInteractionState InteractionState { get; set; } = default!;
    [Inject] private FolderState FolderState { get; set; } = default!;

    private Action? _onInteractionChanged;
    private CalculationMVVM Calc => FolderState.Calculation ?? throw new InvalidOperationException("CalculationToolbar requires an active calculation.");

    private bool CanPasteTasks => TableCoordinator.CanPaste(CalculationItemType.task);

    protected override void OnInitialized()
    {
        _onInteractionChanged = () => _ = InvokeAsync(StateHasChanged);
        InteractionState.Changed += _onInteractionChanged;
    }

    public void Dispose()
    {
        if (_onInteractionChanged != null)
            InteractionState.Changed -= _onInteractionChanged;
    }
}
