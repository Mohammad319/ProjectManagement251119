using AuthPermissions;
using TaskResourceBlueprints;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Infrastructure;
using ProjectManagement.Adminstrator.Components;
using ProjectManagement.Adminstrator.Components.Account;
using ProjectManagement.Adminstrator.Factory;
using ProjectManagement.Adminstrator.Middleware;
using ProjectManagement.Adminstrator.DependencyInjection;
using Serilog;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var taskResourceBlueprintsDb =
    builder.Configuration.GetConnectionString("BlueprintsConnection")
    ?? throw new InvalidOperationException("Connection string 'BlueprintsConnection' (or 'BlueprintsConnection') not found.");

builder.Services.AddTaskResourceBlueprints();
builder.Services.AddDbContextFactory<TaskResourceBlueprintsContext>(options =>
    options.UseSqlServer(taskResourceBlueprintsDb, sqlOptions =>
    {
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));
builder.Services.AddAuthPermissionsLayer();

builder.Services.AddCascadingAuthenticationState();
//builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddApplicationServices();

var connectionString =
    builder.Configuration.GetConnectionString("AuthPermissionsConnection")
    ?? throw new InvalidOperationException("Connection string 'AuthPermissionsConnection' (or fallback 'AuthPermissionsConnection') not found.");

builder.Services.AddCustomAuthentication(connectionString);
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options => options.DetailedErrors = true);

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "sv-SE", "en-US" };
    options.DefaultRequestCulture = new RequestCulture("sv-SE", "en-US");
    options.SupportedCultures = [.. supportedCultures.Select(CultureInfo.GetCultureInfo)];
    options.SupportedUICultures = [.. supportedCultures.Select(CultureInfo.GetCultureInfo)];
    options.ApplyCurrentCultureToResponseHeaders = true;
});

try
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();
}
catch (Exception ex)
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .CreateLogger();

    Log.Warning(ex, "Failed to initialize configured Serilog sinks. Falling back to console logging only.");
}

builder.Host.UseSerilog();

var app = builder.Build();

await app.InitializeAuthPermissionsAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
                return Task.CompletedTask;
            });
        }

        await next();
    });
}
else
{
app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseHsts();
}


app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseRequestLocalization();
app.Use(async (context, next) =>
{
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");
    await next();
});
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();
app.UseMiddleware<GlobalErrorHandling>();
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (ctx, http) =>
    {
        var userId = http.User.FindFirst("UserId")?.Value;
        if (userId is not null) ctx.Set("UserId", userId);
    };
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.Run();
