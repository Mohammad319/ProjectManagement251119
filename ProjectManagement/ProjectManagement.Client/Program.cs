using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ProjectManagement.Client.Configuration;
using ProjectManagement.Client.DependencyInjection;
using ProjectManagement.Client.Handless;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Error;
using ProjectManagement.Client.Shared.Repositories;
using System.Globalization;
using System.Net.Http.Headers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var apiOptions = builder.Configuration.GetSection(ClientApiOptions.SectionName).Get<ClientApiOptions>() ?? new ClientApiOptions();

builder.Logging.SetMinimumLevel(builder.HostEnvironment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information);
builder.Services.Configure<ClientApiOptions>(builder.Configuration.GetSection(ClientApiOptions.SectionName));
builder.Services.AddSingleton(apiOptions);

// Per-user coordinator so the 401 auth-recovery redirect fires once, never as a 429-causing burst.
builder.Services.AddScoped<AuthRedirectState>();
builder.Services.AddScoped<CorrelationIdHandler>();
builder.Services.AddScoped<UnauthorizedRedirectHandler>();
builder.Services.AddScoped<ApiErrorHandler>();

builder.Services.AddScoped<IErrorDialog, UiErrorDialog>();
builder.Services.AddScoped<IClientLogger, ClientLogger>();

builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = apiOptions.ResolveBaseAddress(builder.HostEnvironment);
    client.Timeout = apiOptions.ResolveTimeout();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
.AddHttpMessageHandler<CorrelationIdHandler>()
.AddHttpMessageHandler<UnauthorizedRedirectHandler>()
.AddHttpMessageHandler<ApiErrorHandler>();

builder.Services.AddHttpClient("Log", client =>
{
    client.BaseAddress = apiOptions.ResolveBaseAddress(builder.HostEnvironment);
    client.Timeout = apiOptions.ResolveTimeout();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddClientServices();

var host = builder.Build();

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
    // Culture bootstrap is best effort because prerender/static hosts may not expose JS yet.
}

await host.RunAsync();
