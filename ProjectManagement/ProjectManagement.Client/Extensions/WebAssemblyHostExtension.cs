using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Globalization;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Extensions
{
    public static class WebAssemblyHostExtension
    {
        public static async Task SetDefaultCulture(this WebAssemblyHost host)
        {
            var jsInterop = host.Services.GetRequiredService<IJSRuntime>();
            var uiCultureCode = await jsInterop.InvokeAsync<string>("blazorCulture.get");

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("sv-SE");
            CultureInfo.DefaultThreadCurrentUICulture = string.IsNullOrWhiteSpace(uiCultureCode)
                ? new CultureInfo("en-US")
                : new CultureInfo(uiCultureCode);
        }
    }
}
