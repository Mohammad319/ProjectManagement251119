using AuthPermissions;
using Microsoft.AspNetCore.HttpOverrides;
using ProjectManagement.Configuration;
using ProjectManagement.Extensions;
using ProjectManagement.SignalR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddProjectManagementApp(builder, conn);

var app = builder.Build();

// Ensure AuthPermissions schema/roles are initialized before hosted services start querying tenants.
await app.InitializeAuthPermissionsAsync();

// Pipeline
app.UseProjectManagementPipeline();

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
