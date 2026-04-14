using Microsoft.AspNetCore.Components;

namespace BlazorMHD.UI.Core.Navigation;

public class TabItem
{
    public string Key { get; set; } = Guid.NewGuid().ToString();
    public string? Text { get; set; }
    public string? Icon { get; set; } 
    public RenderFragment? Content { get; set; }
}
