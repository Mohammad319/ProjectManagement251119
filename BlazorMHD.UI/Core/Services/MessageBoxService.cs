using BlazorMHD.UI.Core.DesignSystem;

namespace BlazorMHD.UI.Core.Services;

public class MessageBoxModel
{
    public string Message { get; set; } = "";
    public MhdState State { get; set; } = MhdState.Info;
    public bool AutoHide { get; set; }
    public DateTimeOffset? HideAt { get; set; }
}

public class MessageBoxService
{
    public event Action? OnChange;

    public MessageBoxModel? Current { get; private set; }

    public void Show(string message, MhdState state = MhdState.Info, bool autoHide = false, int seconds = 3)
    {
        Current = new MessageBoxModel
        {
            Message = message,
            State = state,
            AutoHide = autoHide,
            HideAt = autoHide ? DateTimeOffset.UtcNow.AddSeconds(seconds) : null
        };
        OnChange?.Invoke();
    }

    public void Clear()
    {
        Current = null;
        OnChange?.Invoke();
    }

    public void Tick()
    {
        if (Current?.AutoHide == true && Current.HideAt <= DateTimeOffset.UtcNow)
        {
            Current = null;
            OnChange?.Invoke();
        }
    }
}
