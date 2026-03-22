using AuthPermissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using ProjectManagement.Configuration;
using ProjectManagement.Extensions;
using ProjectManagement.HealthChecks;
using ProjectManagement.SignalR;
using Sentry;
using Sentry.Extensibility;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Optional but production-ready crash monitoring.
// With no DSN configured, Sentry stays effectively inactive.
builder.WebHost.UseSentry(options =>
{
    options.SendDefaultPii = false;
    options.AttachStacktrace = true;
    options.MaxRequestBodySize = RequestSize.None;
    options.SetBeforeSend((@event, _) =>
    {
        @event.ServerName = null;

        if (@event.Request?.Headers is not null)
        {
            @event.Request.Headers.Remove("Authorization");
            @event.Request.Headers.Remove("Cookie");
            @event.Request.Headers.Remove("X-Tenant-Secret");
            @event.Request.Headers.Remove("X-Tenant-Reload-Secret");
        }

        return @event;
    });
});

// Logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// Read connection strings
var conn = AppConnectionStringsReader.Read(builder.Configuration);

// Register services
// Reverse proxy support (IIS/Nginx): preserve original scheme/ip to avoid https redirect/prod auth issues.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddProjectManagementApp(builder, conn);

var app = builder.Build();
app.ValidateDeploymentSafety();

// Ensure AuthPermissions schema/roles are initialized before hosted services start querying tenants.
await app.InitializeAuthPermissionsAsync();

// Pipeline
app.UseProjectManagementPipeline();

// Health endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
}).AllowAnonymous();

// Endpoints
app.MapStaticAssets();
app.MapControllers();
app.MapHub<NotificationHub>("/notification");

app.MapRazorComponents<ProjectManagement.Components.App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ProjectManagement.Client._Imports).Assembly);

app.MapAdditionalIdentityEndpoints();

app.Run();
