using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using ProjectManagement.Client.Services.Calculation.CalculationItems;
using ProjectManagement.Client.Shared.MVVM.Calculation;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;

public partial class ResourceRowComponent : CalculationSelectableRowComponentBase
{
    private readonly RenderFragment _resourceCells;

    public ResourceRowComponent() => _resourceCells = RenderResourceCells;

    [Inject] private ResourceService ResourceService { get; set; } = default!;

    [Parameter] public ResourceListMVVM Resource { get; set; } = default!;
    [Parameter] public bool TaskBranchActive { get; set; } = true;
    [Parameter] public string Color { get; set; } = string.Empty;
    [Parameter] public int Left { get; set; }
    [Parameter] public IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> Colmuns { get; set; } = Array.Empty<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>>();

    protected override CalculationItemType SelectionItemType => CalculationItemType.resource;
    protected override int SelectionItemId => Resource.Id;
    protected override decimal? SelectionQuantity => Resource.Quantity;

    private string RowStyle => Resource.Style(Color, TaskBranchActive, IsRowSelected);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ResourceService.OfferStateChanged += HandleOfferStateChanged;
    }

    private void Click() =>
        SelectCurrentItem();

    private Task Context() => Resource.Ui.ContextClick?.Invoke() ?? Task.CompletedTask;

    private void HandleOfferStateChanged(int resourceId)
    {
        if (resourceId != Resource.Id)
            return;

        _ = InvokeAsync(StateHasChanged);
    }

    private void RenderResourceCells(RenderTreeBuilder builder)
        => CalculationRowCellRenderer.RenderResourceCells(builder, Colmuns, Resource);

    public override void Dispose()
    {
        base.Dispose();
        ResourceService.OfferStateChanged -= HandleOfferStateChanged;
    }
}
