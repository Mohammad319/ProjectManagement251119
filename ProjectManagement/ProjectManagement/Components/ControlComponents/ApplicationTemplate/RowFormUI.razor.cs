using System;
using System.Linq;
using Domain.Entities.Application;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.Model.Application;
using ProjectManagement.Client.Shared.ViewModel;
using ProjectManagement.Shared.Base.Application;
using ProjectManagement.Shared.Helper;

namespace ProjectManagement.Components.ControlComponents.ApplicationTemplate;

public partial class RowFormUI
{
    [Parameter] public RowEntity RowParameter { get; set; } = new();
    [Parameter] public int PartPage { get; set; }
    [Parameter] public EventCallback<RowEntity> Callback { get; set; }

    private RowEntity RowUpdate { get; set; } = new();
    private AttributeBase AttributeForm { get; set; } = new();
    private StyleVM Style { get; set; } = new();
    private StyleVM StyleRow { get; set; } = new();
    private bool IsLoading;
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

    private void CallBackField(AttributeBase attr)
    {
        AttributeForm = new AttributeBase();

        if (attr is null)
        {
            return;
        }

        if (attr.ID == Guid.Empty)
        {
            attr.ID = Guid.NewGuid();
            RowUpdate.Attributes.Add(attr);
            return;
        }

        var oldAttr = RowUpdate.Attributes.FirstOrDefault(x => x.ID == attr.ID);
        if (oldAttr != null)
        {
            attr.CopyPropertiesTo(oldAttr);
        }
    }

    private void Remove(AttributeBase attr)
    {
        // Intentionally kept as placeholder for future confirm dialog integration.
    }

    private bool RemoveAsync(AttributeBase st)
    {
        RowUpdate.Attributes.Remove(st);
        StateHasChanged();
        return true;
    }
}
