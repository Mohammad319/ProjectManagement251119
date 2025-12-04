namespace BlazorMHD.UI.Core.Navigation;

public class BreadcrumbItem
{
    public string? Text { get; set; }
    public string? Href { get; set; }
    public bool IsCurrent { get; set; }
}
