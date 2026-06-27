using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.Globalization;

namespace ProjectManagement.Client.Pages.Calculation.Table;

public partial class CalculationToolbar : ComponentBase, IDisposable
{
    [Inject] private ICalculationTableCoordinator TableCoordinator { get; set; } = default!;
    [Inject] private IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;
    [Inject] private CalculationService CalcService { get; set; } = default!;
    [Inject] private CalculationInteractionState InteractionState { get; set; } = default!;
    [Inject] private FolderState FolderState { get; set; } = default!;
    [Inject] private ICalculationRepository CalculationRepository { get; set; } = default!;
    [Inject] private ITemplateRepository TemplateRepository { get; set; } = default!;
    [Inject] private ITemplateColumnRepository TemplateColumnRepository { get; set; } = default!;

    private Action? _onInteractionChanged;
    private List<TemplateMVVM> TemplateOptions { get; set; } = [];
    private List<TemplateColumnMVVM> TemplateColumnOptions { get; set; } = [];
    private bool IsLoadingTemplates { get; set; } = true;
    private bool IsLoadingTemplateColumns { get; set; } = true;
    private bool IsUpdatingTemplate { get; set; }
    private bool IsUpdatingTemplateColumn { get; set; }
    private bool IsUpdatingSort { get; set; }

    private CalculationMVVM Calc => FolderState.Calculation ?? throw new InvalidOperationException("CalculationToolbar requires an active calculation.");

    private bool CanPasteTasks => TableCoordinator.CanPaste(CalculationItemType.task);
    private int? CurrentTemplateId => Calc.TemplateId.GetValueOrDefault() > 0 ? Calc.TemplateId : null;
    private int? CurrentTemplateColumnId => Calc.TemplateColumnId.GetValueOrDefault() > 0 ? Calc.TemplateColumnId : null;
    private bool IsTemplateSelectionDisabled => IsLoadingTemplates || IsUpdatingTemplate;
    private bool IsTemplateColumnSelectionDisabled => IsLoadingTemplateColumns || IsUpdatingTemplateColumn;
    private string SelectedTemplateValue => CurrentTemplateId?.ToString() ?? string.Empty;
    private string SelectedTemplateColumnValue => CurrentTemplateColumnId?.ToString() ?? string.Empty;
    private string SelectedFactorDisplayValue => ((int)Calc.FactorDisplayMode).ToString(CultureInfo.InvariantCulture);
    private string ResourceSortColumnValue => Calc.Sort.ResourceColumn.HasValue
        ? ((int)Calc.Sort.ResourceColumn.Value).ToString(CultureInfo.InvariantCulture)
        : string.Empty;
    private string FactorDisplayTitle => Calc.FactorDisplayMode switch
    {
        CalculationFactorDisplayMode.OH => "OH",
        CalculationFactorDisplayMode.All => $"{CalcResource.netCal} + OH",
        _ => CalcResource.netCal
    };

    protected override void OnInitialized()
    {
        _onInteractionChanged = () => _ = InvokeAsync(StateHasChanged);
        InteractionState.Changed += _onInteractionChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadTemplateOptionsAsync();
        await LoadTemplateColumnOptionsAsync();
    }

    private async Task LoadTemplateOptionsAsync()
    {
        IsLoadingTemplates = true;

        var templates = await TemplateRepository.GetAsync() ?? [];

        if (CurrentTemplateId is int currentTemplateId
            && currentTemplateId > 0
            && templates.All(x => x.Id != currentTemplateId)
            && !string.IsNullOrWhiteSpace(Calc.Template?.Name))
        {
            templates.Insert(0, Calc.Template);
        }

        TemplateOptions =
            [.. templates
                .Where(x => x.Id > 0)
                .GroupBy(x => x.Id)
                .Select(x => x.First())];

        IsLoadingTemplates = false;
    }

    private async Task LoadTemplateColumnOptionsAsync()
    {
        IsLoadingTemplateColumns = true;

        var templateColumns = await TemplateColumnRepository.GetAsync() ?? [];

        if (CurrentTemplateColumnId is int currentTemplateColumnId
            && currentTemplateColumnId > 0
            && templateColumns.All(x => x.Id != currentTemplateColumnId))
        {
            templateColumns.Insert(0, new TemplateColumnMVVM
            {
                Id = currentTemplateColumnId,
                Name = "Current columns"
            });
        }

        TemplateColumnOptions =
            [.. templateColumns
                .Where(x => x.Id > 0)
                .GroupBy(x => x.Id)
                .Select(x => x.First())];

        IsLoadingTemplateColumns = false;
    }

    private async Task OnTemplateChangedAsync(ChangeEventArgs args)
    {
        int? newTemplateId = null;
        var rawValue = args.Value?.ToString();

        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            if (!int.TryParse(rawValue, out var parsedId))
                return;

            newTemplateId = parsedId;
        }

        if (CurrentTemplateId == newTemplateId)
            return;

        IsUpdatingTemplate = true;

        try
        {
            var updatedTemplate = await TemplateRepository.SetDefaultAsync(Calc.Id, newTemplateId);
            Calc.Template = updatedTemplate ?? new TemplateMVVM();
            Calc.TemplateId = newTemplateId;

            if (newTemplateId is int selectedTemplateId
                && selectedTemplateId > 0
                && TemplateOptions.All(x => x.Id != selectedTemplateId)
                && !string.IsNullOrWhiteSpace(Calc.Template.Name))
            {
                TemplateOptions = [Calc.Template, .. TemplateOptions];
            }

            Calc.NotifyGridRefresh();
        }
        finally
        {
            IsUpdatingTemplate = false;
        }
    }

    private async Task OnTemplateColumnChangedAsync(ChangeEventArgs args)
    {
        int? newTemplateColumnId = null;
        var rawValue = args.Value?.ToString();

        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            if (!int.TryParse(rawValue, out var parsedId))
                return;

            newTemplateColumnId = parsedId;
        }

        if (CurrentTemplateColumnId == newTemplateColumnId)
            return;

        IsUpdatingTemplateColumn = true;

        try
        {
            var updatedTemplateColumn = await TemplateColumnRepository.SetDefaultAsync(Calc.Id, newTemplateColumnId);
            Calc.TemplateColumnId = newTemplateColumnId;

            Calc.Template.NetCalc.Columns = updatedTemplateColumn is not null && updatedTemplateColumn.Id > 0
                ? TemplateDefaults.EnsureNetCalcColumns(updatedTemplateColumn.Columns)
                : TemplateDefaults.NetCalc();

            if (newTemplateColumnId is int selectedTemplateColumnId
                && selectedTemplateColumnId > 0
                && TemplateColumnOptions.All(x => x.Id != selectedTemplateColumnId)
                && !string.IsNullOrWhiteSpace(updatedTemplateColumn?.Name))
            {
                TemplateColumnOptions = [updatedTemplateColumn, .. TemplateColumnOptions];
            }

            Calc.NotifyGridRefresh(flatListDirty: true, structureFlatListDirty: true);
        }
        finally
        {
            IsUpdatingTemplateColumn = false;
        }
    }

    private void OnFactorDisplayChanged(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var rawValue))
            return;

        var newMode = (CalculationFactorDisplayMode)rawValue;
        if (!Enum.IsDefined(newMode) || Calc.FactorDisplayMode == newMode)
            return;

        Calc.FactorDisplayMode = newMode;
        CalcService.RequestGridRefresh(CalculationGridRefreshKind.FlatList);
    }

    private async Task OnResourceSortColumnChangedAsync(ChangeEventArgs args)
    {
        Calc.Sort.ResourceColumn = ParseColumnId(args.Value?.ToString());
        await SaveSortAsync();
    }

    private async Task SetResourceSortDescendingAsync(bool descending)
    {
        if (Calc.Sort.ResourceDescending == descending)
            return;

        Calc.Sort.ResourceDescending = descending;
        await SaveSortAsync();
    }

    private async Task SaveSortAsync()
    {
        IsUpdatingSort = true;

        try
        {
            CalcService.RequestGridRefresh(CalculationGridRefreshKind.Structure);
            await CalculationRepository.UpdateSortAsync(Calc.Id, Calc.Sort);
        }
        finally
        {
            IsUpdatingSort = false;
        }
    }

    private static NetColumnId? ParseColumnId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rawValue))
            return null;

        var column = (NetColumnId)rawValue;
        return Enum.IsDefined(column) ? column : null;
    }

    private static string GetSortDirectionClass(bool active) =>
        active
            ? "inline-flex h-5 w-5 items-center justify-center rounded bg-slate-900 text-[11px] text-white disabled:opacity-50 dark:bg-slate-100 dark:text-slate-900"
            : "inline-flex h-5 w-5 items-center justify-center rounded text-[11px] text-slate-500 hover:bg-slate-200 disabled:opacity-50 dark:text-slate-300 dark:hover:bg-slate-700";

    private static readonly (NetColumnId Id, string Label)[] ResourceSortColumns =
    [
        (NetColumnId.Name, "Name"),
        (NetColumnId.Quantity, "Quantity"),
        (NetColumnId.Unit, "Unit"),
        (NetColumnId.Cost, "Cost"),
        (NetColumnId.NetCostQ, "Net cost/Q"),
        (NetColumnId.ChangeFactor1, "Factor 1"),
        (NetColumnId.Account, "Account"),
        (NetColumnId.ResourceTypeSystem, "Resource type"),
        (NetColumnId.Status, "Status")
    ];

    public void Dispose()
    {
        if (_onInteractionChanged != null)
            InteractionState.Changed -= _onInteractionChanged;
    }
}
