using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ProjectManagement.Client.Helper;

namespace ProjectManagement.Client.Shared.Error
{
    public class CustomErrorBoundary : ErrorBoundary
    {
        [Inject] private IWebAssemblyHostEnvironment Env { get; set; } = default!;
        [Inject] private IClientLogger Logger { get; set; } = default!;

        protected override async Task OnErrorAsync(System.Exception exception)
        {
            if (Env.IsDevelopment())
            {
                await base.OnErrorAsync(exception);
                return;
            }

            _ = Logger.ErrorAsync(
                $"Unhandled UI exception: {exception.GetType().Name} — {exception.Message}",
                traceId: null,
                ex: exception);
        }
    }
}
