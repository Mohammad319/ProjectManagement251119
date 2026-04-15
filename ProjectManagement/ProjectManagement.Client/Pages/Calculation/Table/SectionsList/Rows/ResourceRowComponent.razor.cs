using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Services.Calculation.CalculationItems;
using ProjectManagement.Client.Shared.MVVM.Calculation;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;

public partial class ResourceRowComponent : CalculationSelectableRowComponentBase
{
    private readonly RenderFragment _resourceCells;

    public ResourceRowComponent() => _resourceCells = RenderResourceCells;

    [Inject] private CalculationService CalcService { get; set; } = default!;
    [Inject] private ResourceService ResourceService { get; set; } = default!;

    [Parameter] public ResourceListMVVM Resource { get; set; } = default!;
    [Parameter] public bool TaskBranchActive { get; set; } = true;
    [Parameter] public string Color { get; set; } = string.Empty;
    [Parameter] public int Left { get; set; }
    [Parameter] public int MaxFractionDigits { get; set; } = NumericFormatHelper.DefaultMaxFractionDigits;
    [Parameter] public IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> Colmuns { get; set; } = Array.Empty<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>>();

    protected override CalculationItemType SelectionItemType => CalculationItemType.resource;
    protected override int SelectionItemId => Resource.Id;
    protected override decimal? SelectionQuantity => Resource.Quantity;

    private bool IsDetailsOpen { get; set; }

    private string RowStyle => Resource.Style(Color, TaskBranchActive, IsRowSelected);

    private bool HasParameters => Resource?.Data?.Parameters?.Count > 0;
    private bool HasTimes => Resource?.Data?.Times?.Count > 0;
    private bool HasAddOns => Resource?.Data?.AddOns?.Count > 0;
    private bool HasDetails => HasParameters || HasTimes || HasAddOns;
    private bool CanShowDetails => CalcService.ShowResourceVariables && HasDetails;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        CalcService.ResourceVariablesVisibilityChanged += HandleResourceVariablesVisibilityChanged;
        ResourceService.OfferStateChanged += HandleOfferStateChanged;
    }

    private void Click() => SelectCurrentItem();

    private Task Context() => Resource.Ui.ContextClick?.Invoke() ?? Task.CompletedTask;

    private void ToggleDetails()
    {
        if (!CanShowDetails)
            return;

        IsDetailsOpen = !IsDetailsOpen;
    }

    private void HandleOfferStateChanged(int resourceId)
    {
        if (resourceId != Resource.Id)
            return;

        _ = InvokeAsync(StateHasChanged);
    }

    private void HandleResourceVariablesVisibilityChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void RenderResourceCells(RenderTreeBuilder builder)
        => CalculationRowCellRenderer.RenderResourceCells(builder, Colmuns, Resource);

    public override void Dispose()
    {
        base.Dispose();
        CalcService.ResourceVariablesVisibilityChanged -= HandleResourceVariablesVisibilityChanged;
        ResourceService.OfferStateChanged -= HandleOfferStateChanged;
    }
}
