using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;

public abstract class CalculationSelectableRowComponentBase : ComponentBase, IDisposable
{
    private bool _isSelected;

    [Inject] protected CalculationInteractionState InteractionState { get; set; } = default!;

    protected bool IsRowSelected => _isSelected;

    protected abstract CalculationItemType SelectionItemType { get; }
    protected abstract int SelectionItemId { get; }
    protected abstract decimal? SelectionQuantity { get; }

    protected override void OnInitialized()
    {
        InteractionState.SelectionChanged += HandleSelectionChanged;
    }

    protected override void OnParametersSet()
    {
        _isSelected = InteractionState.IsSelected(SelectionItemType, SelectionItemId);
    }

    protected void SelectCurrentItem() =>
        InteractionState.HandleItemSelected(SelectionItemId, SelectionQuantity, SelectionItemType);

    private void HandleSelectionChanged(SelectionChangedEventArgs change)
    {
        if (!change.Affects(SelectionItemType, SelectionItemId))
            return;

        var isSelected = InteractionState.IsSelected(SelectionItemType, SelectionItemId);
        if (isSelected == _isSelected)
            return;

        _isSelected = isSelected;
        _ = InvokeAsync(StateHasChanged);
    }

    public virtual void Dispose()
    {
        InteractionState.SelectionChanged -= HandleSelectionChanged;
    }
}
