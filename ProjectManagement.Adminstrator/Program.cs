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
using ProjectManagement.Client.Adminstrator.DependencyInjection;
using Serilog;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
var taskResourceBlueprintsDb = builder.Configuration.GetConnectionString("BlueprintsDB")
    ?? throw new InvalidOperationException("Connection string 'BlueprintsDB' not found.");
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
builder.Services.AddAuthPermissionsLayer();

var connectionString = builder.Configuration.GetConnectionString("AuthPermissionsDB")
    ?? builder.Configuration.GetConnectionString("AuthPermissions")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'AuthPermissionsDB' not found.");
builder.Services.AddCustomAuthentication(connectionString);
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();


//builder.Services.AddIdentityCore<ApplicationUser>(options =>
//    {
//        options.SignIn.RequireConfirmedAccount = true;
//        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
//    })
//    .AddEntityFrameworkStores<ApplicationDbContext>()
//    .AddSignInManager()
//    .AddDefaultTokenProviders();

//builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options => options.DetailedErrors = true);
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en-US", "se-SE" };
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = [.. supportedCultures.Select(c => new CultureInfo(c))];
    options.SupportedUICultures = [.. supportedCultures.Select(c => new CultureInfo(c))];
});
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Host.UseSerilog();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

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
