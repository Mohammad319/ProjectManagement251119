using BlazorMHD.UI.Core.DesignSystem;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using pax.BlazorChartJs;
using ProjectManagement.Client.DependencyInjection;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

//builder.Services.AddScoped(sp =>
//    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddClientServices();


builder.Services.AddChartJs(options =>
{
    // المواقع الافتراضية (يمكن تركها كما هي)
    options.ChartJsLocation = "https://cdn.jsdelivr.net/npm/chart.js";
    options.ChartJsPluginDatalabelsLocation = "https://cdn.jsdelivr.net/npm/chartjs-plugin-datalabels@2";
});
builder.Services.AddChartJs();

builder.Services.BlazorMHD();

var host = builder.Build();

var js = host.Services.GetRequiredService<IJSRuntime>();
var cultureName = await js.InvokeAsync<string>("blazorCulture.get");

CultureInfo culture;
if (!string.IsNullOrWhiteSpace(cultureName))
{
    culture = new CultureInfo(cultureName);
}
else
{
    culture = new CultureInfo("en-US"); // الافتراضي
}

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();