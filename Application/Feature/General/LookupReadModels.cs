namespace Application.Feature.General;

public sealed class LookupAdminListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#00ff00";
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; }
}
