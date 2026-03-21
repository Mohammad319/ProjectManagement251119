using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ViewModel;
using ProjectManagement.Shared.DTO.App;
using ProjectManagement.Shared.Helper;

namespace ProjectManagement.Components.ControlComponents.ApplicationTemplate;

public partial class RowFormUI
{
    [Parameter] public RowDTO RowParameter { get; set; } = new();
    [Parameter] public int PartPage { get; set; }
    [Parameter] public EventCallback<RowDTO> Callback { get; set; }

    private RowDTO RowUpdate { get; set; } = new();
    private AttributeDTO AttributeForm { get; set; } = new();
    private StyleVM Style { get; set; } = new();
    private StyleVM StyleRow { get; set; } = new();
    private bool IsLoading { get; set; }
    private string StyleStr { get; set; } = string.Empty;
    private string StyleRowStr { get; set; } = string.Empty;
    private int Part { get; set; }

    protected override void OnInitialized()
    {
        Style.Set(RowParameter.Style);
        StyleRow.Set(RowParameter.StyleRow);
        StyleStr = Style.GetString();
        StyleRowStr = StyleRow.GetString();
        PropertyCopier.CopyPropertiesTo(RowParameter, RowUpdate);
    }

    private void updateStyle()
    {
        StyleStr = Style.GetString();
        StyleRowStr = StyleRow.GetString();
    }

    private async Task HandleSubmitAsync()
    {
        RowUpdate.Style = Style.GetString();
        RowUpdate.StyleRow = StyleRow.GetString();
        await Callback.InvokeAsync(RowUpdate);
    }

    private void CallBackField(AttributeDTO attr)
    {
        AttributeForm = new AttributeDTO();

        if (attr is null)
            return;

        if (attr.ID == Guid.Empty)
        {
            attr.ID = Guid.NewGuid();
            RowUpdate.Attributes.Add(attr);
            return;
        }

        var oldAttr = RowUpdate.Attributes.FirstOrDefault(x => x.ID == attr.ID);
        if (oldAttr != null)
            attr.CopyPropertiesTo(oldAttr);
    }

    private void Remove(AttributeDTO attr)
    {
        // Intentionally kept as placeholder for future confirm dialog integration.
    }

    private bool RemoveAsync(AttributeDTO st)
    {
        RowUpdate.Attributes.Remove(st);
        StateHasChanged();
        return true;
    }
}
