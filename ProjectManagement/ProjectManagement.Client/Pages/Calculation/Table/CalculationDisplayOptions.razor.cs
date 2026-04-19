using BlazorMHD.UI.Core.Services;
using BlazorMHD.UI.Core.DesignSystem;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Client.Pages.Calculation.Table.Header;

namespace ProjectManagement.Client.Pages.Calculation.Table;

public partial class CalculationDisplayOptions : ComponentBase
{
    [Inject] private IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;
    [Inject] private ICalculationTableCoordinator TableCoordinator { get; set; } = default!;
    [Inject] private CalculationService CalcService { get; set; } = default!;
    [Inject] private FolderState FolderState { get; set; } = default!;
    [Inject] private ICalculationRepository CalcRepo { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;

    private bool IsOpen { get; set; }
    private CalculationMVVM Calc => FolderState.Calculation ?? throw new InvalidOperationException("CalculationDisplayOptions requires an active calculation.");

    private void ToggleDropdown() => IsOpen = !IsOpen;

    private void NotifyStructureRefresh() => TableCoordinator.NotifyStructureRefresh();

    private void OpenPresetsDialog()
    {
        IsOpen = false;
        DialogService.ShowComponent<CalculationDisplayOptionsPresetsDialog>(
            AppLoc["displayPresets"],
            new Dictionary<string, object>
            {
                [nameof(CalculationDisplayOptionsPresetsDialog.Store)] = Calc.DisplayPresets,
                [nameof(CalculationDisplayOptionsPresetsDialog.OnApply)] = EventCallback.Factory.Create<DisplayOptionsPreset>(this, ApplyPreset),
                [nameof(CalculationDisplayOptionsPresetsDialog.OnSave)] = EventCallback.Factory.Create<DisplayOptionsPresetStore>(this, SavePresetsAsync),
                [nameof(CalculationDisplayOptionsPresetsDialog.OnClear)] = EventCallback.Factory.Create(this, ClearPresetOverrides),
            },
            DialogSize.Medium);
    }

    private void ApplyPreset(DisplayOptionsPreset preset)
    {
        Calc.ShowTasks = preset.ShowTasks;
        Calc.ShowResources = preset.ShowResources;
        CalcService.ShowComments = preset.ShowComments;
        CalcService.ShowResourceVariables = preset.ShowResourceVariables;
        Calc.OnlyActive = preset.OnlyActive;
        Calc.ApplyPresetActiveOverrides(preset);
        NotifyStructureRefresh();
        StateHasChanged();
    }

    private void ClearPresetOverrides()
    {
        Calc.ClearPresetActiveOverrides();
        NotifyStructureRefresh();
        StateHasChanged();
    }

    private async Task SavePresetsAsync(DisplayOptionsPresetStore store)
    {
        await CalcRepo.UpdateDisplayPresetsAsync(Calc.Id, store);
    }
}
