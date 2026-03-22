using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using ProjectManagement.Client.Services.Calculation.CalculationItems;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Shared.Calculation;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;

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
    private bool _disposed;
    private static readonly IReadOnlyDictionary<NetColumnId, int> DefaultWidths =
        TemplateDefaults.NetCalc().ToDictionary(x => x.Id, x => x.Width);
    private CalculationMVVM Calc => _observedCalculation ?? throw new InvalidOperationException("CalcDataGrid requires an active calculation.");
    private TemplateMVVM Template => _observedTemplate ?? throw new InvalidOperationException("CalcDataGrid requires an active template.");

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
            return;

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

        RefreshColumns();
        Template.FreezCol();
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
            return currentCalculation is not null && _observedTemplate is not null;

        if (_observedCalculation is not null && _onChangeHandler is not null)
            _observedCalculation.OnChangeInCalculation -= _onChangeHandler;

        _observedCalculation = currentCalculation;
        _observedTemplate = currentCalculation?.Template;

        if (_observedCalculation is not null && _onChangeHandler is not null)
            _observedCalculation.OnChangeInCalculation += _onChangeHandler;

        if (_observedCalculation is null || _observedTemplate is null)
            return false;

        RefreshColumns();
        _observedTemplate.FreezCol();
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
        if (_disposed || _observedCalculation is null || _observedTemplate is null)
            return;

        if (_reloadPending)
            return;

        _reloadPending = true;

        try
        {
            await Task.Yield();

            if (_disposed || _observedCalculation is null || _observedTemplate is null)
                return;

            EnsureColumnsUpToDate();
            bool refreshItems = Calc.AllFlatItems == null || Calc.FlatListDirty;

            if (refreshItems)
            {
                Calc.AllFlatItems = Calc.BuildFlatList();
                Calc.FlatListDirty = false;

                if (Calc.MaxDepth > 0)
                    Template.StartCol1 = (Calc.MaxDepth * 10) + 15;
            }

            if (refreshItems && virtualizeComponent != null)
                await virtualizeComponent.RefreshDataAsync();

            if (refreshItems)
                CalcService.NotifyGridViewMaterialized();

            _jsSyncPending = true;
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

        Template.FreezCol();
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

        if (Calc.MaxDepth > 0)
            Template.StartCol1 = (Calc.MaxDepth * 10) + 15;

        if (virtualizeComponent != null)
            await virtualizeComponent.RefreshDataAsync();

        CalcService.NotifyGridViewMaterialized();
        _jsSyncPending = true;
        await InvokeAsync(StateHasChanged);
    }

    private string GetColumnSignature() =>
        Template?.NetCalc?.Columns is { Count: > 0 } columns
            ? string.Join(',', columns.Select(x => $"{(int)x.Id}:{x.Width}"))
            : string.Empty;

    private void RefreshColumns()
    {
        var templateColumns = Template?.NetCalc?.Columns;
        var round = Template?.MathRound ?? 0;
        Columns = CalcColumnFactory.GetColumns(Calc.Tax, round, templateColumns);
        HeaderColumns = BuildHeaderColumns(Columns, templateColumns);
        _jsSyncPending = true;
    }

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
}
