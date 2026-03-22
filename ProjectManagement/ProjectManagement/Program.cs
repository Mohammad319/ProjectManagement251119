using AuthPermissions;
using AuthPermissions.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using ProjectManagement.Configuration;
using ProjectManagement.Extensions;
using ProjectManagement.SignalR;
using Microsoft.EntityFrameworkCore;
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
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddProjectManagementApp(builder, conn);

var app = builder.Build();


// Surface production-safety warnings early in the logs.
app.LogProductionConfigurationWarnings(conn);

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

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }))
    .AllowAnonymous()
    .WithTags("Health");

app.MapGet("/health/ready", async (IServiceProvider services, CancellationToken ct) =>
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var canConnect = await db.Database.CanConnectAsync(ct);

            if (!canConnect)
            {
                return Results.Problem(
                    title: "Database unavailable",
                    detail: "The application is running, but the primary database is not reachable.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Ok(new { status = "ready" });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Startup dependency check failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    })
    .AllowAnonymous()
    .WithTags("Health");

app.Run();
