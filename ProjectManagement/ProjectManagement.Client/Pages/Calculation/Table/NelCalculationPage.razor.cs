using BlazorMHD.UI.Core.DesignSystem;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Pages.Calculation.Table.Header;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ViewModel;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System.Globalization;
using System.Linq;

namespace ProjectManagement.Client.Pages.Calculation.Table;

public partial class NelCalculationPage : ComponentBase, IDisposable
{
    [Inject] private IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;
    [Inject] private IStringLocalizer<ResourceStorage> StorageLoc { get; set; } = default!;
    [Inject] private CalculationService CalcService { get; set; } = default!;
    [Inject] private CalculationInteractionState InteractionState { get; set; } = default!;
    [Inject] private FolderState FolderState { get; set; } = default!;
    [Inject] private ICalculationRepository CalcRepo { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;

    private Action? _onFolderChanged;
    private Action? _onInteractionChanged;
    private Action? _onCommentsVisibilityChanged;
    private Action? _onResourceVariablesVisibilityChanged;
    private Action? _onGridViewMaterialized;
    private Action? _onCalculationChanged;
    private CalculationMVVM? _observedCalculation;

    private CalculationMVVM? Calc => FolderState.Calculation;
    private TemplateMVVM? Template => Calc?.Template;

    private bool HasCalculation => Calc is not null;
    private int TaskCount => Calc?.Tasks.Count(task => task.Type != TaskType.CodeName) ?? 0;
    private int OnlyCodeTextTaskCount => Calc?.Tasks.Count(task => task.Type == TaskType.CodeName) ?? 0;
    private int TotalTaskCount => Calc?.Tasks.Count ?? 0;
    private int ResourceCount => Calc?.ResourceById.Count ?? 0;
    private int SelectedItemCount => InteractionState.SelectedItems.Count;
    private bool HasFlatListSnapshot => Calc is not null && (Calc.AllFlatItems is not null || TotalTaskCount == 0);
    private int VisibleItemCount => Calc?.AllFlatItems?.Count ?? 0;
    private string VisibleItemCountText => HasFlatListSnapshot ? VisibleItemCount.ToString(CultureInfo.InvariantCulture) : "...";
    private bool ShowEmptyState => Calc is not null && TotalTaskCount == 0;
    private bool ShowNoVisibleRowsState => Calc is not null && !ShowEmptyState && HasFlatListSnapshot && VisibleItemCount == 0;
    private bool HasActiveFilters => HasVisibleFilter(Calc?.FilterVM);
    private string NoVisibleRowsHint => HasActiveFilters
        ? AppLoc["calculationTableNoResultsHint"]
        : AppLoc["calculationTableNoVisibleRowsHint"];
    private static string ActiveToggleChipClass => "calc-status-chip calc-status-chip-toggle is-active";
    private static string InactiveToggleChipClass => "calc-status-chip calc-status-chip-toggle is-muted";

    protected override void OnInitialized()
    {
        _onFolderChanged = HandleFolderChanged;
        FolderState.OnChange += _onFolderChanged;

        _onInteractionChanged = () => _ = InvokeAsync(StateHasChanged);
        InteractionState.Changed += _onInteractionChanged;

        _onCommentsVisibilityChanged = () => _ = InvokeAsync(StateHasChanged);
        CalcService.CommentsVisibilityChanged += _onCommentsVisibilityChanged;

        _onResourceVariablesVisibilityChanged = () => _ = InvokeAsync(StateHasChanged);
        CalcService.ResourceVariablesVisibilityChanged += _onResourceVariablesVisibilityChanged;

        _onGridViewMaterialized = () => _ = InvokeAsync(StateHasChanged);
        CalcService.GridViewMaterialized += _onGridViewMaterialized;

        _onCalculationChanged = () => _ = InvokeAsync(StateHasChanged);
        ObserveCalculation();
    }

    private void HandleFolderChanged()
    {
        ObserveCalculation();
        ApplyActivePresetIfExists();
        _ = InvokeAsync(StateHasChanged);
    }

    private void ApplyActivePresetIfExists()
    {
        if (Calc is null) return;
        var active = DisplayOptionsPresetState.GetActivePreset(Calc.DisplayPresets);
        if (active is not null)
            Calc.ApplyPresetActiveOverrides(active);
        else
            Calc.ClearPresetActiveOverrides();

        Calc.ExecuteCalculation();
    }

    private void ObserveCalculation()
    {
        if (ReferenceEquals(_observedCalculation, Calc))
            return;

        if (_observedCalculation is not null && _onCalculationChanged is not null)
            _observedCalculation.OnChangeInCalculation -= _onCalculationChanged;

        _observedCalculation = Calc;

        if (_observedCalculation is not null && _onCalculationChanged is not null)
            _observedCalculation.OnChangeInCalculation += _onCalculationChanged;
    }

    private void ClearPresetOverrides()
    {
        if (Calc is null) return;
        Calc.ClearPresetActiveOverrides();
        Calc.ExecuteCalculation();
        CalcService.RequestGridRefresh(CalculationGridRefreshKind.FlatList);
        StateHasChanged();
    }

    private void ClearFilters()
    {
        if (Calc is null)
            return;

        CalcService.GetFilter(null);
        Calc.FilterVM = null;
        StateHasChanged();
    }

    private void ClearSelection() => InteractionState.ResetSelection();

    private void ToggleShowTasks()
    {
        if (Calc is null)
            return;

        Calc.ShowTasks = !Calc.ShowTasks;
        CalcService.RequestGridRefresh(CalculationGridRefreshKind.FlatList);
    }

    private void ToggleShowOnlyCodeTextTasks()
    {
        if (Calc is null)
            return;

        Calc.ShowOnlyCodeTextTasks = !Calc.ShowOnlyCodeTextTasks;
        CalcService.RequestGridRefresh(CalculationGridRefreshKind.FlatList);
    }

    private void ToggleShowResources()
    {
        if (Calc is null)
            return;

        Calc.ShowResources = !Calc.ShowResources;
        CalcService.RequestGridRefresh(CalculationGridRefreshKind.FlatList);
    }

    private void ToggleComments() => CalcService.ShowComments = !CalcService.ShowComments;

    private void ToggleResourceVariables() => CalcService.ShowResourceVariables = !CalcService.ShowResourceVariables;

    private void ToggleOnlyActive()
    {
        if (Calc is null)
            return;

        Calc.OnlyActive = !Calc.OnlyActive;
        CalcService.RequestGridRefresh(CalculationGridRefreshKind.FlatList);
    }

    private void OpenDisplayPresetsDialog()
    {
        if (Calc is null) return;

        DialogService.ShowComponent<CalculationDisplayOptionsPresetsDialog>(
            AppLoc["displayPresets"],
            new Dictionary<string, object>
            {
                [nameof(CalculationDisplayOptionsPresetsDialog.Store)] = Calc.DisplayPresets,
                [nameof(CalculationDisplayOptionsPresetsDialog.OnApply)] = EventCallback.Factory.Create<DisplayOptionsPreset>(this, ApplyDisplayPreset),
                [nameof(CalculationDisplayOptionsPresetsDialog.OnSave)] = EventCallback.Factory.Create<DisplayOptionsPresetStore>(this, SaveDisplayPresetsAsync),
                [nameof(CalculationDisplayOptionsPresetsDialog.OnClear)] = EventCallback.Factory.Create(this, ClearPresetOverrides),
            },
            DialogSize.Medium);
    }

    private void ApplyDisplayPreset(DisplayOptionsPreset preset)
    {
        if (Calc is null) return;
        Calc.ShowTasks = preset.ShowTasks;
        Calc.ShowResources = preset.ShowResources;
        CalcService.ShowComments = preset.ShowComments;
        CalcService.ShowResourceVariables = preset.ShowResourceVariables;
        Calc.OnlyActive = preset.OnlyActive;
        Calc.ApplyPresetActiveOverrides(preset);
        Calc.ExecuteCalculation();
        CalcService.RequestGridRefresh(CalculationGridRefreshKind.FlatList);
        StateHasChanged();
    }

    private async Task SaveDisplayPresetsAsync(DisplayOptionsPresetStore store)
    {
        if (Calc is null) return;
        await CalcRepo.UpdateDisplayPresetsAsync(Calc.Id, store);
    }

    private static string GetToggleChipClass(bool isActive) =>
        isActive ? ActiveToggleChipClass : InactiveToggleChipClass;

    public void Dispose()
    {
        if (_onFolderChanged is not null)
            FolderState.OnChange -= _onFolderChanged;

        if (_onInteractionChanged is not null)
            InteractionState.Changed -= _onInteractionChanged;

        if (_onCommentsVisibilityChanged is not null)
            CalcService.CommentsVisibilityChanged -= _onCommentsVisibilityChanged;

        if (_onResourceVariablesVisibilityChanged is not null)
            CalcService.ResourceVariablesVisibilityChanged -= _onResourceVariablesVisibilityChanged;

        if (_onGridViewMaterialized is not null)
            CalcService.GridViewMaterialized -= _onGridViewMaterialized;

        if (_observedCalculation is not null && _onCalculationChanged is not null)
            _observedCalculation.OnChangeInCalculation -= _onCalculationChanged;
    }

    private static bool HasVisibleFilter(FilterVM? filter)
    {
        if (filter is null)
            return false;

        return filter.Code.Count > 0
            || filter.Name.Count > 0
            || filter.Account.Count > 0
            || filter.Status.Count > 0
            || filter.ResourceTypeId > 0
            || filter.Resource.Count > 0
            || filter.ResourceTypeSystem.Count > 0
            || filter.ResourceSortId.HasValue
            || filter.ResourceSort.Count > 0
            || filter.Unit.Count > 0;
    }
}
