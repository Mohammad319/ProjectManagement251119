using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Services.Calculation.CalculationItems;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;

public partial class ResourceRowComponent : CalculationSelectableRowComponentBase
{
    private readonly RenderFragment _resourceCells;

    public ResourceRowComponent() => _resourceCells = RenderResourceCells;

    [Inject] private CalculationService CalcService { get; set; } = default!;
    [Inject] private ResourceService ResourceService { get; set; } = default!;

    [Parameter] public ResourceListMVVM Resource { get; set; } = default!;
    [Parameter] public bool TaskBranchActive { get; set; } = true;
    [Parameter] public bool IsItemActive { get; set; } = true;
    [Parameter] public bool ShowActiveToggle { get; set; }
    [Parameter] public EventCallback OnToggleActive { get; set; }
    [Parameter] public NetColor? Colors { get; set; }
    [Parameter] public int Left { get; set; }
    [Parameter] public int MaxFractionDigits { get; set; } = NumericFormatHelper.DefaultMaxFractionDigits;
    [Parameter] public IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> Colmuns { get; set; } = Array.Empty<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>>();

    protected override CalculationItemType SelectionItemType => CalculationItemType.resource;
    protected override int SelectionItemId => Resource.Id;
    protected override decimal? SelectionQuantity => Resource.Quantity;

    private bool IsDetailsOpen { get; set; }

    private string ResolvedColor => Colors?.Resource ?? TemplateConstBase.Resource;
    private string InactiveTextColor => Colors?.InactiveText ?? TemplateConstBase.InactiveText;
    private string RowStyle => Resource.Style(ResolvedColor, InactiveTextColor, TaskBranchActive, IsRowSelected);

    private bool HasParameters => Resource?.Data?.Parameters?.Count > 0;
    private bool HasTimes => Resource?.Data?.Times?.Count > 0;
    private bool HasAddOns => Resource?.Data?.AddOns?.Count > 0;
    private bool HasDetails => HasParameters || HasTimes || HasAddOns;
    private bool CanShowDetails => CalcService.ShowResourceVariables && HasDetails;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        CalcService.ResourceVariablesVisibilityChanged += HandleResourceVariablesVisibilityChanged;
        CalcService.ResourceDetailsExpandChanged += HandleResourceDetailsExpandChanged;
        ResourceService.OfferStateChanged += HandleOfferStateChanged;
    }

    private void Click(MouseEventArgs e) => SelectCurrentItem(e);

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

    private void HandleResourceDetailsExpandChanged(bool expand)
    {
        if (!CanShowDetails)
            return;
        IsDetailsOpen = expand;
        _ = InvokeAsync(StateHasChanged);
    }

    private void RenderResourceCells(RenderTreeBuilder builder)
        => CalculationRowCellRenderer.RenderResourceCells(builder, Colmuns, Resource, Left, ShowActiveToggle, ToggleActive, TaskBranchActive, EditProductionNote);

    private Task ToggleActive() => OnToggleActive.InvokeAsync();

    private Task EditProductionNote()
    {
        CalcService.EditProductionNote(
            CalculationItemType.resource,
            Resource.Id,
            Resource.ProductionNote,
            v => { Resource.ProductionNote = v; InvokeAsync(StateHasChanged); });
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        base.Dispose();
        CalcService.ResourceVariablesVisibilityChanged -= HandleResourceVariablesVisibilityChanged;
        CalcService.ResourceDetailsExpandChanged -= HandleResourceDetailsExpandChanged;
        ResourceService.OfferStateChanged -= HandleOfferStateChanged;
    }
}
