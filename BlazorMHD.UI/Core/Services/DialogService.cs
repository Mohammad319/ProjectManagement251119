using Microsoft.AspNetCore.Components;
using BlazorMHD.UI.Core.DesignSystem;

namespace BlazorMHD.UI.Core.Services;

public class DialogButtonModel
{
    public string Text { get; set; } = "";
    public MhdState State { get; set; } = MhdState.Neutral;
    public bool IsPrimary { get; set; }
    public MhdSize Size { get; set; } = MhdSize.Md;
    public EventCallback? OnClick { get; set; }
}

public enum DialogSize
{
    Small,
    Medium,
    Large,
    ExtraLarge,
    FullScreen
}

public class DialogModel
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Icon { get; set; }

    public RenderFragment? Content { get; set; }
    public List<DialogButtonModel> Buttons { get; set; } = new();

    public bool IsDraggable { get; set; } = true;
    public bool CloseOnOverlayClick { get; set; } = true;

    public DialogSize Size { get; set; } = DialogSize.Medium;
    public MhdState State { get; set; } = MhdState.Neutral;

    public string? WidthClass { get; set; }
    public string? MaxHeightClass { get; set; } = "max-h-[80vh]";

    // إزاحة السحب لكل نافذة
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
}


public class DialogService
{
    public event Action? OnChange;

    private readonly List<DialogModel> _dialogs = new();
    public IReadOnlyList<DialogModel> Dialogs => _dialogs;

    public void Show(DialogModel model)
    {
        _dialogs.Add(model);
        OnChange?.Invoke();
    }

    // إغلاق آخر نافذة (أعلى واحدة)
    public void Close()
    {
        if (_dialogs.Count == 0)
            return;

        _dialogs.RemoveAt(_dialogs.Count - 1);
        OnChange?.Invoke();
    }

    // إغلاق نافذة معيّنة
    public void Close(DialogModel model)
    {
        if (_dialogs.Remove(model))
            OnChange?.Invoke();
    }

    public void ShowComponent<TComponent>(
        string title,
        Dictionary<string, object>? parameters = null,
        DialogSize size = DialogSize.Large)
        where TComponent : IComponent
    {
        ShowComponent<TComponent>(title, "", parameters, size);
    }

    public void ShowComponent<TComponent>(
        string title,
        string icon,
        Dictionary<string, object>? parameters = null,
        DialogSize size = DialogSize.Large)
        where TComponent : IComponent
    {
        RenderFragment content = builder =>
        {
            builder.OpenComponent(0, typeof(TComponent));

            if (parameters != null)
            {
                var seq = 1;
                foreach (var kvp in parameters)
                {
                    builder.AddAttribute(seq++, kvp.Key, kvp.Value);
                }
            }

            builder.CloseComponent();
        };

        Show(new DialogModel
        {
            Title = title,
            Icon = icon,
            Content = content,
            Size = size
        });
    }

    public void ShowSimple(string title, string message, string? okText = null)
    {
        var model = new DialogModel
        {
            Title = title,
            Content = builder =>
            {
                builder.OpenElement(0, "div");
                builder.OpenElement(1, "p");
                builder.AddAttribute(2, "class", "text-xs text-slate-500 dark:text-slate-400");
                builder.AddContent(3, message);
                builder.CloseElement();
                builder.CloseElement();
            },
            Buttons = new()
            {
                new DialogButtonModel
                {
                    Text = okText ?? "OK",
                    State = MhdState.Primary,
                    IsPrimary = true
                }
            },
            CloseOnOverlayClick = true
        };

        Show(model);
    }
}
