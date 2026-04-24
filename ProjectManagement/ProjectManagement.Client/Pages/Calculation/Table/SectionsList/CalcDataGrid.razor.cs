using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services.Calculation.CalculationItems;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Shared.Calculation;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.ViewModel;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.Globalization;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList;

public partial class CalcDataGrid : ComponentBase, IDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private CalculationService CalcService { get; set; } = default!;
    [Inject] private CalculationInteractionState InteractionState { get; set; } = default!;
    [Inject] private FolderState FolderState { get; set; } = default!;
    [Inject] private ITemplateRepository TemplateRepository { get; set; } = default!;
    [Inject] private TaskService TaskService { get; set; } = default!;
    [Inject] private ResourceService ResourceService { get; set; } = default!;
    [Inject] private ICalculationRepository CalcRepo { get; set; } = default!;

    private Virtualize<FlatItem>? virtualizeComponent;
    private DotNetObjectReference<CalcDataGrid>? _dotNetRef;
    private IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> Columns = Array.Empty<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>>();
    private IReadOnlyList<ColumnHeader> HeaderColumns = Array.Empty<ColumnHeader>();
    private bool TH1ISvisible;
    private Action? _onChangeHandler;
    private CalculationMVVM? _observedCalculation;
    private TemplateMVVM? _observedTemplate;
    private decimal _lastTax;
    private int _lastRound;
    private string _lastColumnSignature = string.Empty;
    private bool _reloadPending;
    private bool _jsSyncPending = true;
    private bool _collapseScrollClampPending;
    private int _virtualizeRenderKey;
    private bool _disposed;

    private static readonly IReadOnlyDictionary<NetColumnId, int> DefaultWidths =
        TemplateDefaults.NetCalc().ToDictionary(x => x.Id, x => x.Width);

    private CalculationMVVM Calc => _observedCalculation ?? throw new InvalidOperationException("CalcDataGrid requires an active calculation.");
    private TemplateMVVM Template => _observedTemplate ?? throw new InvalidOperationException("CalcDataGrid requires an active template.");

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
            return;

        if (_collapseScrollClampPending)
        {
            _collapseScrollClampPending = false;
            await JS.InvokeVoidAsync("clampCalcGridScroll");
        }

        if (!firstRender && !_jsSyncPending)
            return;

        _dotNetRef ??= DotNetObjectReference.Create(this);
        _jsSyncPending = false;
        await JS.InvokeVoidAsync("initializeResizableColumns", _dotNetRef);
    }

    protected override void OnInitialized()
    {
        _onChangeHandler = () => _ = InvokeAsync(ForceReload);

        if (!ObserveCalculation())
            throw new InvalidOperationException("CalcDataGrid requires an active calculation.");

        _lastTax = Calc.Tax;
        _lastRound = Template.MathRound;
        _lastColumnSignature = GetColumnSignature();

        TH1ISvisible = true;

        CalcService.RequestGridRefresh(CalculationGridRefreshKind.FlatList);
    }

    protected override void OnParametersSet()
    {
        if (_disposed)
            return;

        ObserveCalculation();
    }
    private bool ObserveCalculation()
    {
        var currentCalculation = FolderState.Calculation;
        if (ReferenceEquals(_observedCalculation, currentCalculation))
            return SyncObservedTemplate();

        if (_observedCalculation is not null && _onChangeHandler is not null)
            _observedCalculation.OnChangeInCalculation -= _onChangeHandler;

        _observedCalculation = currentCalculation;
        _observedTemplate = currentCalculation?.Template;

        if (_observedCalculation is not null && _onChangeHandler is not null)
            _observedCalculation.OnChangeInCalculation += _onChangeHandler;

        return SyncObservedTemplate();
    }

    private bool SyncObservedTemplate()
    {
        if (_observedCalculation is null)
        {
            _observedTemplate = null;
            return false;
        }

        var currentTemplate = _observedCalculation.Template;
        if (currentTemplate is null)
        {
            _observedTemplate = null;
            return false;
        }

        var templateChanged = !ReferenceEquals(_observedTemplate, currentTemplate);
        _observedTemplate = currentTemplate;

        if (_observedTemplate.StartCol1 <= 0)
            _observedTemplate.StartCol1 = GetStartColumnWidth(_observedCalculation.MaxDepth);

        if (templateChanged || Columns.Count == 0)
        {
            RefreshColumns();
            _lastRound = _observedTemplate.MathRound;
            _lastColumnSignature = GetColumnSignature();
        }

        return true;
    }

    private void EnsureColumnsUpToDate()
    {
        var currentSignature = GetColumnSignature();
        if (_lastTax != Calc.Tax || _lastRound != Template.MathRound || _lastColumnSignature != currentSignature || Columns.Count == 0)
        {
            _lastTax = Calc.Tax;
            _lastRound = Template.MathRound;
            _lastColumnSignature = currentSignature;
            RefreshColumns();
        }
    }

    private async Task ForceReload()
    {
        if (_disposed || !SyncObservedTemplate())
            return;

        if (_reloadPending)
            return;

        _reloadPending = true;

        try
        {
            await Task.Yield();

            if (_disposed || !SyncObservedTemplate())
                return;

            EnsureColumnsUpToDate();
            bool refreshItems = Calc.AllFlatItems == null || Calc.FlatListDirty;

            if (refreshItems)
            {
                Calc.AllFlatItems = Calc.BuildFlatList();
                Calc.FlatListDirty = false;
                _virtualizeRenderKey++;
                var needed = GetStartColumnWidth(Calc.MaxDepth);
                if (Template.StartCol1 < needed)
                    Template.StartCol1 = needed;
            }

            if (refreshItems && virtualizeComponent != null)
                await virtualizeComponent.RefreshDataAsync();

            if (refreshItems)
                CalcService.NotifyGridViewMaterialized();

            _jsSyncPending = true;
            _collapseScrollClampPending = true;
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            _reloadPending = false;
        }
    }

    private ValueTask<ItemsProviderResult<FlatItem>> LoadItems(ItemsProviderRequest request)
    {
        var list = Calc.AllFlatItems;
        if (list == null || list.Count == 0)
            return new(new ItemsProviderResult<FlatItem>(Array.Empty<FlatItem>(), 0));

        int start = request.StartIndex;
        if ((uint)start >= (uint)list.Count)
            return new(new ItemsProviderResult<FlatItem>(Array.Empty<FlatItem>(), list.Count));

        int count = Math.Min(request.Count, list.Count - start);
        IReadOnlyList<FlatItem> slice = new ListSlice<FlatItem>(list, start, count);
        EnsureRowActionsBound(slice);
        return new(new ItemsProviderResult<FlatItem>(slice, list.Count));
    }

    [JSInvokable]
    public async Task SaveTemplateBlazor(string payload)
    {
        if (_disposed || _observedCalculation is null || _observedTemplate is null)
            return;

        if (!CalculationTableResizeHelper.TryParseResizePayload(payload, out int headerIndex, out int newWidth))
            return;

        if (headerIndex == 1)
        {
            Template.StartCol1 = newWidth;
        }
        else
        {
            var visibleColumnIds = HeaderColumns.Select(x => x.Id).ToArray();
            if (!CalculationTableResizeHelper.TryResolveColumnId(headerIndex, visibleColumnIds, out var columnId))
                return;

            var templateColumn = Template.NetCalc.Columns.FirstOrDefault(x => x.Id == columnId);
            if (templateColumn == null)
                return;

            templateColumn.Width = newWidth;
        }

        RefreshColumns();
        CalcService.RequestGridRefresh(CalculationGridRefreshKind.View);
        _jsSyncPending = true;
        await InvokeAsync(StateHasChanged);

        if (Calc.TemplateId > 0)
        {
            TemplateListPostDTO temp = new();
            Template.CopyPropertiesTo(temp);
            await TemplateRepository.UpdateAsync(temp, Calc.TemplateId.Value);
        }
    }

    protected void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Control" or "Shift" or "Alt")
            InteractionState.SetModifierKey(e.Key);
    }

    protected void HandleKeyUp(KeyboardEventArgs _) => InteractionState.ClearModifierKey();



    private string GetFilterToggleButtonClass() =>
        Calc.FilterVM is null
            ? "calc-filter-toggle-button"
            : "calc-filter-toggle-button is-active";
    private void ShFilter()
    {
        if (Calc.FilterVM == null)
            Calc.FilterVM = new();
        else
        {
            CalcService.GetFilter(null);
            Calc.FilterVM = null;
        }
    }
    private bool HasActivePreset => DisplayOptionsPresetState.GetActivePreset(Calc.DisplayPresets) is not null;

    private async Task ToggleTaskActiveById(int taskId)
    {
        if (_disposed || _observedCalculation is null) return;
        Calc.ToggleTaskActiveInPreset(taskId, CalcService.ShowComments, CalcService.ShowResourceVariables);
        await RefreshAndSavePresetsAsync();
    }

    private async Task ToggleResourceActiveById(int resourceId)
    {
        if (_disposed || _observedCalculation is null) return;
        Calc.ToggleResourceActiveInPreset(resourceId, CalcService.ShowComments, CalcService.ShowResourceVariables);
        await RefreshAndSavePresetsAsync();
    }

    private async Task RefreshAndSavePresetsAsync()
    {
        Calc.ExecuteCalculation();
        Calc.AllFlatItems = Calc.BuildFlatList();
        Calc.FlatListDirty = false;
        _virtualizeRenderKey++;
        _collapseScrollClampPending = true;
        if (virtualizeComponent != null)
            await virtualizeComponent.RefreshDataAsync();
        await InvokeAsync(StateHasChanged);
        await CalcRepo.UpdateDisplayPresetsAsync(Calc.Id, Calc.DisplayPresets);
    }

    private async Task ToggleCollSpan(TaskListMVVM task)
    {
        if (_disposed || _observedCalculation is null || _observedTemplate is null)
            return;

        if (!Calc.TryToggleTaskCollapse(task))
        {
            task.Ui.CollSpan = !task.Ui.CollSpan;
            CalcService.RequestGridRefresh(CalculationGridRefreshKind.Structure);
            return;
        }

        Calc.AllFlatItems = Calc.BuildFlatList();
        Calc.FlatListDirty = false;
        _virtualizeRenderKey++;
        var neededWidth = GetStartColumnWidth(Calc.MaxDepth);
        if (Template.StartCol1 < neededWidth)
            Template.StartCol1 = neededWidth;

        if (virtualizeComponent != null)
            await virtualizeComponent.RefreshDataAsync();

        CalcService.NotifyGridViewMaterialized();
        _jsSyncPending = true;
        _collapseScrollClampPending = true;
        await InvokeAsync(StateHasChanged);
    }

    private string GetColumnSignature() =>
        Template?.NetCalc?.Columns is { Count: > 0 } columns
            ? string.Join(',', columns.Select(x => $"{(int)x.Id}:{x.Width}"))
            : string.Empty;

    private static int GetStartColumnWidth(int maxDepth)
    {
        const int indentPerLevel = 10;  // matches Left="@(item.Depth * 10)"
        const int toggleSize = 20;      // w-5 h-5 = 20px (expand/collapse button)
        const int flexGap = 4;          // gap-1 between indent and toggle
        const int breathing = 8;        // extra padding for comfortable click area
        const int minWidth = 50;        // minimum even with no nesting

        int computed = (Math.Max(maxDepth, 0) * indentPerLevel) + toggleSize + flexGap + breathing;
        return Math.Max(minWidth, computed);
    }

    private void RefreshColumns()
    {
        var templateColumns = Template?.NetCalc?.Columns;
        var round = Template?.MathRound ?? 0;
        Columns = CalcColumnFactory.GetColumns(Calc.Tax, round, templateColumns);
        HeaderColumns = BuildHeaderColumns(Columns, templateColumns);
        _jsSyncPending = true;
    }

    private string GetHeaderCssClass(int index)
    {
        var baseClass = $"colH{index + 2} draggable-table";
        return IsFrozenHeader(HeaderColumns[index].Id)
            ? $"{baseClass} pm-frozen-candidate"
            : baseClass;
    }

    private string GetFrozenHeaderAttribute(NetColumnId id) =>
        IsFrozenHeader(id) ? "true" : "false";

    private bool IsFrozenHeader(NetColumnId id) =>
        Template?.NetCalc?.Columns?.FirstOrDefault(x => x.Id == id)?.Frozen == true;

    private static IReadOnlyList<ColumnHeader> BuildHeaderColumns(
        IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> columns,
        IReadOnlyList<NetColumnState>? templateColumns)
    {
        if (columns.Count == 0)
            return Array.Empty<ColumnHeader>();

        var widths = new Dictionary<NetColumnId, int>(templateColumns?.Count ?? 0);
        if (templateColumns is not null)
        {
            for (int i = 0; i < templateColumns.Count; i++)
                widths[templateColumns[i].Id] = templateColumns[i].Width;
        }

        var headers = new ColumnHeader[columns.Count];
        for (int i = 0; i < columns.Count; i++)
        {
            var id = columns[i].Id;
            if (!widths.TryGetValue(id, out var width) && !DefaultWidths.TryGetValue(id, out width))
                width = 80;

            headers[i] = new ColumnHeader(id, width);
        }

        return headers;
    }

    private void EnsureRowActionsBound(IReadOnlyList<FlatItem>? items)
    {
        if (items is null || items.Count == 0)
            return;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item.IsTask)
            {
                var task = item.Task!;
                task.Ui.ContextClick ??= () => TaskService.ContextMenu(task);
                continue;
            }

            if (!item.IsResource)
                continue;

            var resource = item.Resource!;
            resource.Ui.ContextClick ??= () => ResourceService.Context(resource, resource.TaskId);
            resource.Ui.OfferClick ??= () => ResourceService.HandleOfferAsync(resource);
        }
    }

    private bool TryBuildSummaryTotals(out SummaryTotals totals)
    {
        if (!Calc.ShowTasks)
        {
            totals = default;
            return false;
        }

        var flatItems = Calc.AllFlatItems;
        if (flatItems is { Count: > 0 })
            return TryBuildSummaryTotals(flatItems, out totals);

        if (Calc.RootTasks.Count == 0)
        {
            totals = default;
            return false;
        }

        decimal totalNetCost = 0m;
        decimal priceTotally = 0m;
        decimal priceTotallyTax = 0m;
        decimal baseCost = 0m;
        decimal priceTotalSub = 0m;
        decimal diff = 0m;
        double totalCo2 = 0d;
        decimal priceActuallyQuantity = 0m;
        decimal priceWorkedQ = 0m;
        decimal priceActuallyQuantityTax = 0m;
        decimal priceWorkedQTax = 0m;
        decimal priceTotalSubTax = 0m;
        decimal quantitySum = 0m;
        string? commonUnit = null;
        bool unitMismatch = false;
        bool hasTasks = false;

        var tax = Calc.Tax;

        for (int i = 0; i < Calc.RootTasks.Count; i++)
        {
            var task = Calc.RootTasks[i];
            if (!ShouldIncludeSummaryTask(task))
                continue;

            hasTasks = true;
            totalNetCost += task.GetComputedNetCostTotaly();
            priceTotally += task.GetComputedApriceTotally();
            priceTotallyTax += task.ApriceTotallyTax(tax);
            baseCost += task.BaseCost ?? 0m;
            priceTotalSub += task.PriceSubTotal;
            diff += task.Diff;
            totalCo2 += task.TotalCO2 ?? 0d;
            priceActuallyQuantity += task.PriceActuallyQuantity;
            priceWorkedQ += task.PriceWorkedQ;
            priceActuallyQuantityTax += task.PriceActuallyQuantityTax(tax);
            priceWorkedQTax += task.PriceWorkedQTax(tax);
            priceTotalSubTax += task.PriceTotalSubTax(tax);

            if (!unitMismatch && task.Quantity.HasValue)
            {
                var unit = task.Unit ?? string.Empty;
                if (commonUnit is null)
                    commonUnit = unit;
                else if (commonUnit != unit)
                    unitMismatch = true;

                if (!unitMismatch)
                    quantitySum += task.Quantity.Value;
            }
        }

        var qSum = (!unitMismatch && commonUnit is not null) ? quantitySum : (decimal?)null;
        totals = new SummaryTotals(totalNetCost, priceTotally, priceTotallyTax,
            baseCost, priceTotalSub, diff, totalCo2,
            priceActuallyQuantity, priceWorkedQ, priceActuallyQuantityTax, priceWorkedQTax, priceTotalSubTax,
            qSum);
        return hasTasks;
    }

    private bool TryBuildSummaryTotals(IReadOnlyList<FlatItem> flatItems, out SummaryTotals totals)
    {
        decimal totalNetCost = 0m;
        decimal priceTotally = 0m;
        decimal baseCost = 0m;
        decimal priceTotalSub = 0m;
        double totalCo2 = 0d;
        decimal priceActuallyQuantity = 0m;
        decimal priceWorkedQ = 0m;
        decimal quantitySum = 0m;
        string? commonUnit = null;
        bool unitMismatch = false;
        bool hasItems = false;

        var tax = Calc.Tax;
        bool sumFromResources = Calc.ShowResources;

        for (int i = 0; i < flatItems.Count; i++)
        {
            var item = flatItems[i];

            if (sumFromResources && item.IsResource && item.Resource is not null)
            {
                var res = item.Resource;
                hasItems = true;
                totalNetCost += res.GetComputedNetCostTotaly();
                priceTotally += res.GetComputedApriceTotally();
                baseCost += res.GetComputedBaseCost() ?? 0m;
                priceTotalSub += res.PriceSubTotal;
                totalCo2 += res.GetComputedTotalCO2() ?? 0d;
            }

            if (item.IsTask && item.Depth == 0 && item.Task is not null)
            {
                var task = item.Task;

                if (!sumFromResources)
                {
                    hasItems = true;
                    totalNetCost += task.GetComputedNetCostTotaly();
                    priceTotally += task.GetComputedApriceTotally();
                    baseCost += task.BaseCost ?? 0m;
                    priceTotalSub += task.PriceSubTotal;
                    totalCo2 += task.TotalCO2 ?? 0d;
                }
                else
                {
                    hasItems = true;
                }

                priceActuallyQuantity += task.PriceActuallyQuantity;
                priceWorkedQ += task.PriceWorkedQ;

                if (!unitMismatch && task.Quantity.HasValue)
                {
                    var unit = task.Unit ?? string.Empty;
                    if (commonUnit is null)
                        commonUnit = unit;
                    else if (commonUnit != unit)
                        unitMismatch = true;

                    if (!unitMismatch)
                        quantitySum += task.Quantity.Value;
                }
            }
        }

        if (!hasItems)
        {
            totals = default;
            return false;
        }

        decimal diff = priceTotalSub - priceTotally;
        decimal priceTotallyTax = priceTotally * (1m + (tax / 100m));
        decimal priceActuallyQuantityTax = priceActuallyQuantity * (1m + (tax / 100m));
        decimal priceWorkedQTax = priceWorkedQ * (1m + (tax / 100m));
        decimal priceTotalSubTax = priceTotalSub * (1m + (tax / 100m));

        var qSum = (!unitMismatch && commonUnit is not null) ? quantitySum : (decimal?)null;
        totals = new SummaryTotals(totalNetCost, priceTotally, priceTotallyTax,
            baseCost, priceTotalSub, diff, totalCo2,
            priceActuallyQuantity, priceWorkedQ, priceActuallyQuantityTax, priceWorkedQTax, priceTotalSubTax,
            qSum);
        return true;
    }

    private bool ShouldIncludeSummaryTask(TaskListMVVM task) =>
        task.Ui.FilterVisible &&
        Calc.MatchesFactorDisplay(task.IsOH) &&
        (!Calc.OnlyActive || task.Active);

    private string GetSummaryCellValue(NetColumnId columnId, SummaryTotals totals) =>
        columnId switch
        {
            NetColumnId.TotalNetCost => FormatSummaryValue(totals.TotalNetCost),
            NetColumnId.PriceTotaly => FormatSummaryValue(totals.PriceTotally),
            NetColumnId.PriceTotallyTax => FormatSummaryValue(totals.PriceTotallyTax),
            NetColumnId.BaseCost => FormatSummaryValue(totals.BaseCost),
            NetColumnId.PriceTotalSub => FormatSummaryValue(totals.PriceTotalSub),
            NetColumnId.Diff => FormatSummaryValue(totals.Diff),
            NetColumnId.TotalCo2 => FormatSummaryDouble(totals.TotalCo2),
            NetColumnId.PriceActuallyQuantity => FormatSummaryValue(totals.PriceActuallyQuantity),
            NetColumnId.PriceWorkedQ => FormatSummaryValue(totals.PriceWorkedQ),
            NetColumnId.PriceActuallyQuantityTax => FormatSummaryValue(totals.PriceActuallyQuantityTax),
            NetColumnId.PriceWorkedQTax => FormatSummaryValue(totals.PriceWorkedQTax),
            NetColumnId.PriceTotalSubTax => FormatSummaryValue(totals.PriceTotalSubTax),
            NetColumnId.Quantity => totals.QuantitySum.HasValue ? FormatSummaryValue(totals.QuantitySum.Value) : string.Empty,
            _ => string.Empty
        };

    private static bool IsSummaryValueColumn(NetColumnId columnId) =>
        columnId is NetColumnId.TotalNetCost
            or NetColumnId.PriceTotaly
            or NetColumnId.PriceTotallyTax
            or NetColumnId.BaseCost
            or NetColumnId.PriceTotalSub
            or NetColumnId.Diff
            or NetColumnId.TotalCo2
            or NetColumnId.PriceActuallyQuantity
            or NetColumnId.PriceWorkedQ
            or NetColumnId.PriceActuallyQuantityTax
            or NetColumnId.PriceWorkedQTax
            or NetColumnId.PriceTotalSubTax
            or NetColumnId.Quantity;

    private NetColumnId? GetSummaryLabelColumnId()
    {
        for (int i = 0; i < HeaderColumns.Count; i++)
        {
            if (HeaderColumns[i].Id == NetColumnId.Name)
                return NetColumnId.Name;
        }

        for (int i = 0; i < HeaderColumns.Count; i++)
        {
            if (HeaderColumns[i].Id == NetColumnId.Account)
                return NetColumnId.Account;
        }

        for (int i = 0; i < HeaderColumns.Count; i++)
        {
            var columnId = HeaderColumns[i].Id;
            if (!IsSummaryValueColumn(columnId))
                return columnId;
        }

        return null;
    }

    private static string GetSummaryCellCssClass(NetColumnId columnId, NetColumnId? labelColumnId)
    {
        if (columnId == labelColumnId)
            return "calc-summary-label-cell";

        return IsSummaryValueColumn(columnId) ? "num-cell" : string.Empty;
    }

    private string FormatSummaryValue(decimal value) =>
        NumericFormatHelper.Format(value, Template.MathRound, CultureInfo.CurrentCulture);

    private string FormatSummaryDouble(double value) =>
        NumericFormatHelper.Format((decimal)value, Template.MathRound, CultureInfo.CurrentCulture);

    private bool _isUpdatingSort;

    private static readonly HashSet<NetColumnId> TaskSortableColumns =
    [
        NetColumnId.Name,
        NetColumnId.Code,
        NetColumnId.Quantity,
        NetColumnId.Unit,
        NetColumnId.NetCostQ,
        NetColumnId.TotalNetCost,
        NetColumnId.PriceTotaly,
        NetColumnId.ChangeFactor1,
        NetColumnId.ChangeFactor2,
        NetColumnId.Status,
        NetColumnId.Responsible
    ];

    private bool IsTaskSortableColumn(NetColumnId id) =>
        Calc.Tap1 && TaskSortableColumns.Contains(id);

    private bool IsTaskSortActive(NetColumnId id, bool descending) =>
        Calc.Sort.TaskColumn == id && Calc.Sort.TaskDescending == descending;

    private string GetTaskSortClass(NetColumnId id, bool descending) =>
        IsTaskSortActive(id, descending)
            ? "text-[10px] leading-none rounded bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 px-0.5 cursor-pointer disabled:opacity-50"
            : "text-[10px] leading-none rounded text-slate-400 hover:text-slate-600 dark:text-slate-500 dark:hover:text-slate-200 px-0.5 cursor-pointer disabled:opacity-50";

    private async Task ToggleTaskSortAsync(NetColumnId columnId, bool descending)
    {
        if (_isUpdatingSort) return;

        if (Calc.Sort.TaskColumn == columnId && Calc.Sort.TaskDescending == descending)
            Calc.Sort.TaskColumn = null;
        else
        {
            Calc.Sort.TaskColumn = columnId;
            Calc.Sort.TaskDescending = descending;
        }

        _isUpdatingSort = true;
        try
        {
            CalcService.RequestGridRefresh(CalculationGridRefreshKind.Structure);
            await CalcRepo.UpdateSortAsync(Calc.Id, Calc.Sort);
        }
        finally
        {
            _isUpdatingSort = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        _disposed = true;

        if (_observedCalculation is not null && _onChangeHandler != null)
            _observedCalculation.OnChangeInCalculation -= _onChangeHandler;

        _dotNetRef?.Dispose();
    }

    private sealed class ListSlice<T>(List<T> list, int start, int count) : IReadOnlyList<T>
    {
        private readonly List<T> _list = list;
        private readonly int _start = start;
        public int Count { get; } = count;

        public T this[int index] => _list[_start + index];

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return _list[_start + i];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private readonly record struct ColumnHeader(NetColumnId Id, int Width);
    private readonly record struct SummaryTotals(
        decimal TotalNetCost,
        decimal PriceTotally,
        decimal PriceTotallyTax,
        decimal BaseCost,
        decimal PriceTotalSub,
        decimal Diff,
        double TotalCo2,
        decimal PriceActuallyQuantity,
        decimal PriceWorkedQ,
        decimal PriceActuallyQuantityTax,
        decimal PriceWorkedQTax,
        decimal PriceTotalSubTax,
        decimal? QuantitySum);
}
