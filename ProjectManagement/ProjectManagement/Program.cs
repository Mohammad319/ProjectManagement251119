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
builder.Services.AddProjectManagementApp(builder, conn);

var app = builder.Build();
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
