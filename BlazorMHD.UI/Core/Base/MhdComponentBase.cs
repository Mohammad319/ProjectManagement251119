using Microsoft.AspNetCore.Components;

namespace BlazorMHD.UI.Core.Base;

public class MhdComponentBase : ComponentBase, IDisposable
{
    private bool _isDisposed;

    protected void SafeRender()
    {
        if (_isDisposed) return;
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _isDisposed = true;
        OnDispose();
    }

    protected virtual void OnDispose() { }
}
