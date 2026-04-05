using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using ProjectManagement.Client.DependencyInjection;
using ProjectManagement.Client.Handless;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Error;
using ProjectManagement.Client.Shared.Repositories;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;

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
//builder.Services.AddOidcAuthentication(options =>
//{
//    options.UserOptions.RoleClaim = ClaimTypes.Role;
//});

var host = builder.Build();

// ✅ حماية culture حتى لا يكسر التشغيل لو JS غير موجود
try
{
    var js = host.Services.GetRequiredService<IJSRuntime>();
    var uiCultureName = await js.InvokeAsync<string>("blazorCulture.get");

    CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("sv-SE");
    CultureInfo.DefaultThreadCurrentUICulture = !string.IsNullOrWhiteSpace(uiCultureName)
        ? new CultureInfo(uiCultureName)
        : new CultureInfo("en-US");
}
catch
{
    // ignore
}

await host.RunAsync();
