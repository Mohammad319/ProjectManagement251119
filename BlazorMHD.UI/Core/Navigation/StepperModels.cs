namespace BlazorMHD.UI.Core.Navigation;

public class StepItem
{
    public string Key { get; set; } = Guid.NewGuid().ToString();
    public string? Title { get; set; }
    public string? Description { get; set; }
}
