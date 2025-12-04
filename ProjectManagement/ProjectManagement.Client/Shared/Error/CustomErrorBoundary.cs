using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Error
{
    public class CustomErrorBoundary: ErrorBoundary
    {
        [Inject] private IWebAssemblyHostEnvironment env { get; set; } = default!;
        protected override Task OnErrorAsync(System.Exception exception)
        {
            if(env.IsDevelopment())
                return base.OnErrorAsync(exception);
            return Task.CompletedTask;
        }
    }
}
