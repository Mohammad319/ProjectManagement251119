using Microsoft.AspNetCore.Components;

namespace BlazorMHD.UI.Core.Navigation;

public class DrawerItem
{
    public string Key { get; set; } = Guid.NewGuid().ToString();
    public string? Text { get; set; }
    public string? Icon { get; set; }
    public string? Href { get; set; }
    public bool IsActive { get; set; }
    public EventCallback OnClick { get; set; }
}
