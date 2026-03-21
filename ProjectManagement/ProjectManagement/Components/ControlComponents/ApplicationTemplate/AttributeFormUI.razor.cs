using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ViewModel;
using ProjectManagement.Shared;
using ProjectManagement.Shared.Base.Application;
using ProjectManagement.Shared.DTO.App;
using ProjectManagement.Shared.Helper;
using System.Text.Json;

namespace ProjectManagement.Components.ControlComponents.ApplicationTemplate;

public partial class AttributeFormUI
{
    [Parameter] public AttributeDTO Attribute { get; set; } = new();
    [Parameter] public EventCallback<AttributeDTO> Callback { get; set; }

    private int PartPage { get; set; }
    private AttributeDTO AttributeUpdate { get; set; } = new();
    private StyleVM Style { get; set; } = new();
    private string StyleStr { get; set; } = string.Empty;
    private bool IsLoading;
    private ValidSelectVM ValidSelectVM { get; set; } = new();
    private string SelectV { get; set; } = string.Empty;
    private ValidTextVM ValidTextVM { get; set; } = new();
    private ValidNumberVM ValidNumberVM { get; set; } = new();
    private ValidBoolVM ValidBoolVM { get; set; } = new();
    private ValidDateVM ValidDateVM { get; set; } = new();
    private bool ShowSelect { get; set; } = true;

    protected override void OnInitialized()
    {
        Style.Set(Attribute.Style);
        StyleStr = Attribute.Style;
        PropertyCopier.CopyPropertiesTo(Attribute, AttributeUpdate);

        if (AttributeUpdate.ID == Guid.Empty)
        {
            PartPage = 0;
            return;
        }

        PartPage = 1;

        if (string.IsNullOrEmpty(AttributeUpdate.Validation))
            return;

        if (AttributeUpdate.AttributeType is AttributeType.Int or AttributeType.Double)
        {
            ValidNumberVM = JsonSerializer.Deserialize<ValidNumberVM>(AttributeUpdate.Validation) ?? new ValidNumberVM();
        }
        else if (AttributeUpdate.AttributeType is AttributeType.Text or AttributeType.TextArea)
        {
            ValidTextVM = JsonSerializer.Deserialize<ValidTextVM>(AttributeUpdate.Validation) ?? new ValidTextVM();
        }
        else if (AttributeUpdate.AttributeType == AttributeType.Select)
        {
            ValidSelectVM = JsonSerializer.Deserialize<ValidSelectVM>(AttributeUpdate.Validation) ?? new ValidSelectVM();
        }
        else if (AttributeUpdate.AttributeType == AttributeType.Bool)
        {
            ValidBoolVM = JsonSerializer.Deserialize<ValidBoolVM>(AttributeUpdate.Validation) ?? new ValidBoolVM();
        }
        else if (AttributeUpdate.AttributeType is AttributeType.DateTime or AttributeType.Date or AttributeType.Time)
        {
            ValidDateVM = JsonSerializer.Deserialize<ValidDateVM>(AttributeUpdate.Validation) ?? new ValidDateVM();
        }
    }

    private void Refresh()
    {
        StyleStr = Style.GetString();
    }

    private async Task HandleSubmitAsync()
    {
        IsLoading = true;
        AttributeUpdate.Style = Style.GetString();

        if (AttributeUpdate.AttributeType == AttributeType.Select)
        {
            AttributeUpdate.Validation = JsonSerializer.Serialize(ValidSelectVM);
            SelectV = string.Empty;
        }
        else if (AttributeUpdate.AttributeType is AttributeType.Text or AttributeType.TextArea)
        {
            AttributeUpdate.Validation = JsonSerializer.Serialize(ValidTextVM);
        }
        else if (AttributeUpdate.AttributeType is AttributeType.Int or AttributeType.Double)
        {
            AttributeUpdate.Validation = JsonSerializer.Serialize(ValidNumberVM);
        }
        else if (AttributeUpdate.AttributeType == AttributeType.Bool)
        {
            AttributeUpdate.Validation = JsonSerializer.Serialize(ValidBoolVM);
        }
        else if (AttributeUpdate.AttributeType is AttributeType.DateTime or AttributeType.Date or AttributeType.Time)
        {
            AttributeUpdate.Validation = JsonSerializer.Serialize(ValidDateVM);
        }

        await Callback.InvokeAsync(AttributeUpdate);
        IsLoading = false;
    }

    private async Task CancelAsync()
    {
        await Callback.InvokeAsync(null);
    }

    private async Task RemoveSelect(int i)
    {
        if (i < 0 || i >= ValidSelectVM.Values.Count)
            return;

        ShowSelect = false;
        await Task.Delay(1);
        ValidSelectVM.Values.RemoveAt(i);
        ShowSelect = true;
    }
}
