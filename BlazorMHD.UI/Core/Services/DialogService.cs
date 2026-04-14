using BlazorMHD.UI.Core.DesignSystem;
using Microsoft.AspNetCore.Components;

namespace BlazorMHD.UI.Core.Services;

public class DialogButtonModel
{
    public string Text { get; set; } = "";
    public MhdState State { get; set; } = MhdState.Neutral;
    public bool IsPrimary { get; set; }
    public MhdSize Size { get; set; } = MhdSize.Md;
    public EventCallback? OnClick { get; set; }

    public string Type { get; set; } = "button";   // "button" | "submit"
    public string? FormId { get; set; }             // used when Type == "submit"
    public bool AutoClose { get; set; } = true;     // close dialog after click (for button type)
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
    public string Id { get; init; } = $"mhd-dlg-{Guid.NewGuid():N}";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Icon { get; set; }

    public RenderFragment? Content { get; set; }
    public List<DialogButtonModel> Buttons { get; set; } = new();

    public bool IsDraggable { get; set; } = true;
    public bool CloseOnOverlayClick { get; set; } = true;

    // عند false تظهر الصفحة الخلفية كما هي تماما
    public bool UseOverlay { get; set; } = true;

    // تُستخدم فقط عند UseOverlay = true
    public double OverlayOpacity { get; set; } = 0.45;

    // callback after dialog is closed and removed from stack
    public Func<Task>? OnClosed { get; set; }

    public DialogSize Size { get; set; } = DialogSize.Medium;
    public MhdState State { get; set; } = MhdState.Neutral;

    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public bool HasExplicitPosition { get; set; }
    public ElementReference SurfaceRef { get; set; }
}

public class DialogService
{
    public event Action? OnChange;

    private readonly List<DialogModel> _dialogs = new();
    public IReadOnlyList<DialogModel> Dialogs => _dialogs;

    public void Show(DialogModel model)
    {
        model.OverlayOpacity = Math.Clamp(model.OverlayOpacity, 0d, 1d);
        _dialogs.Add(model);
        OnChange?.Invoke();
    }

    // للتوافق مع الاستدعاءات القديمة
    public void Close() => _ = CloseAsync();

    // للتوافق مع الاستدعاءات القديمة
    public void Close(DialogModel model) => _ = CloseAsync(model);

    public async Task CloseAsync()
    {
        if (_dialogs.Count == 0)
            return;

        var model = _dialogs[^1];
        _dialogs.RemoveAt(_dialogs.Count - 1);
        OnChange?.Invoke();

        if (model.OnClosed is not null)
            await model.OnClosed();
    }

    public async Task CloseAsync(DialogModel? model)
    {
        if (model is null)
            return;

        if (!_dialogs.Remove(model))
            return;

        OnChange?.Invoke();

        if (model.OnClosed is not null)
            await model.OnClosed();
    }

    public void ShowComponent<TComponent>(
        string title,
        Dictionary<string, object>? parameters = null,
        DialogSize size = DialogSize.Large,
        List<DialogButtonModel>? btns = null,
        Func<Task>? onClosed = null,
        bool closeOnOverlayClick = true,
        bool isDraggable = true,
        bool useOverlay = true,
        double overlayOpacity = 0.45)
        where TComponent : IComponent
    {
        ShowComponent<TComponent>(
            title,
            "",
            parameters,
            size,
            btns,
            onClosed,
            closeOnOverlayClick,
            isDraggable,
            useOverlay,
            overlayOpacity);
    }

    public void ShowComponent<TComponent>(
        string title,
        string icon,
        Dictionary<string, object>? parameters = null,
        DialogSize size = DialogSize.Large,
        List<DialogButtonModel>? btns = null,
        Func<Task>? onClosed = null,
        bool closeOnOverlayClick = true,
        bool isDraggable = true,
        bool useOverlay = true,
        double overlayOpacity = 0.45)
        where TComponent : IComponent
    {
        RenderFragment content = builder =>
        {
            builder.OpenComponent(0, typeof(TComponent));

            if (parameters is not null)
            {
                var seq = 1;
                foreach (var kvp in parameters)
                    builder.AddAttribute(seq++, kvp.Key, kvp.Value);
            }

            builder.CloseComponent();
        };

        Show(new DialogModel
        {
            Title = title,
            Icon = icon,
            Content = content,
            Size = size,
            Buttons = btns ?? [],
            CloseOnOverlayClick = closeOnOverlayClick,
            IsDraggable = isDraggable,
            UseOverlay = useOverlay,
            OverlayOpacity = overlayOpacity,
            OnClosed = onClosed
        });
    }

    public void ShowSimple(
        string title,
        string message,
        string? okText = null,
        bool useOverlay = true,
        double overlayOpacity = 0.45,
        Func<Task>? onClosed = null)
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
                    IsPrimary = true,
                    AutoClose = true
                }
            },
            CloseOnOverlayClick = true,
            UseOverlay = useOverlay,
            OverlayOpacity = overlayOpacity,
            OnClosed = onClosed
        };

        Show(model);
    }
}
