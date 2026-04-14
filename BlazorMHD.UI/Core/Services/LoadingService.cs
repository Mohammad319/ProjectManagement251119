namespace BlazorMHD.UI.Core.Services;

public class LoadingService
{
    public event Action? OnChange;

    private int _counter;

    public bool IsBusy => _counter > 0;

    public void Begin()
    {
        _counter++;
        OnChange?.Invoke();
    }

    public void End()
    {
        if (_counter > 0)
        {
            _counter--;
            OnChange?.Invoke();
        }
    }

    public IDisposable Scope() => new LoadingScope(this);

    private class LoadingScope : IDisposable
    {
        private readonly LoadingService _svc;
        private bool _disposed;

        public LoadingScope(LoadingService svc)
        {
            _svc = svc;
            _svc.Begin();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _svc.End();
        }
    }
}
