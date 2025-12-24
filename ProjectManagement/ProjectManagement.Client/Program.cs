using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using ProjectManagement.Client.DependencyInjection;
using ProjectManagement.Client.Handless;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Error;
using ProjectManagement.Client.Shared.Repositories;
using System.Globalization;
using System.Net.Http.Headers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ✅ Handlers + Dialog + ClientLogger
builder.Services.AddScoped<CorrelationIdHandler>();
builder.Services.AddScoped<UnauthorizedRedirectHandler>();
builder.Services.AddScoped<ApiErrorHandler>();

builder.Services.AddScoped<IErrorDialog, UiErrorDialog>();
builder.Services.AddScoped<IClientLogger, ClientLogger>();

//// ✅ HttpClientFactory + named client Api

builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})

.AddHttpMessageHandler<CorrelationIdHandler>()
.AddHttpMessageHandler<UnauthorizedRedirectHandler>()
.AddHttpMessageHandler<ApiErrorHandler>()
;
builder.Services.AddHttpClient("Log", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

//builder.Services.AddScoped(sp =>sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddClientServices();

var host = builder.Build();

// ✅ حماية culture حتى لا يكسر التشغيل لو JS غير موجود
try
{
    var js = host.Services.GetRequiredService<IJSRuntime>();
    var cultureName = await js.InvokeAsync<string>("blazorCulture.get");

    var culture = !string.IsNullOrWhiteSpace(cultureName)
        ? new CultureInfo(cultureName)
        : new CultureInfo("en-US");

    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;
}
catch
{
    // ignore
}

await host.RunAsync();
