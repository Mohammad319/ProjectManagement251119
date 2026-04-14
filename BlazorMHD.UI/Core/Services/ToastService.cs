using BlazorMHD.UI.Core.DesignSystem;

namespace BlazorMHD.UI.Core.Services;

public class ToastModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public MhdState State { get; set; } = MhdState.Info;
    public DateTimeOffset ExpireAt { get; set; } = DateTimeOffset.UtcNow.AddSeconds(4);
}

public class ToastService : IDisposable
{
    private readonly List<ToastModel> _toasts = new();
    private readonly System.Timers.Timer _timer;

    public event Action? OnChange;

    public ToastService()
    {
        _timer = new System.Timers.Timer(5);
        _timer.AutoReset = true;
        _timer.Elapsed += (_, _) => CleanupExpired();
        _timer.Start();
    }

    public void Show(string title, string message, MhdState state = MhdState.Info, int seconds = 4)
    {
        _toasts.Add(new ToastModel
        {
            Title = title,
            Message = message,
            State = state,
            ExpireAt = DateTimeOffset.UtcNow.AddSeconds(seconds)
        });

        OnChange?.Invoke();
    }

    public IReadOnlyList<ToastModel> GetToasts() => _toasts.ToList();

    public void Dismiss(Guid id)
    {
        var t = _toasts.FirstOrDefault(x => x.Id == id);
        if (t is not null)
        {
            _toasts.Remove(t);
            OnChange?.Invoke();
        }
    }

    private void CleanupExpired()
    {
        var removed = _toasts.RemoveAll(t => t.ExpireAt <= DateTimeOffset.UtcNow);
        if (removed > 0)
            OnChange?.Invoke();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
