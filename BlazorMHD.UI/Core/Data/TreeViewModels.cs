namespace BlazorMHD.UI.Core.Data;

public class TreeNode<TItem>
{
    public string Key { get; set; } = Guid.NewGuid().ToString();
    public string? Text { get; set; }
    public TItem? Value { get; set; }

    public bool IsExpanded { get; set; }
    public bool IsSelected { get; set; }

    public List<TreeNode<TItem>> Children { get; set; } = new();

    public bool HasChildren => Children is { Count: > 0 };
}
