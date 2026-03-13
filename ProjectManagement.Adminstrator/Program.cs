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
    builder.Configuration.GetConnectionString("TaskResourceBlueprintsConnection")
    ?? builder.Configuration.GetConnectionString("TaskResourceBlueprintsDb")
    ?? throw new InvalidOperationException("Connection string 'TaskResourceBlueprintsConnection' (or 'TaskResourceBlueprintsDb') not found.");

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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

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
    var supportedCultures = new[] { "en-US", "sv-SE" };
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = [.. supportedCultures.Select(c => new CultureInfo(c))];
    options.SupportedUICultures = [.. supportedCultures.Select(c => new CultureInfo(c))];
});

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Host.UseSerilog();

var app = builder.Build();

await app.InitializeAuthPermissionsAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseRequestLocalization();
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
