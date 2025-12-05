using Application;
using AuthPermissions;
using AuthPermissions.Context;
using BlazorMHD.UI.Core.DesignSystem;
using BlazorMHD.UI.Core.Services;
using Domain.Settings;
using TaskResourceBlueprints;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using TaskResourceBlueprints.Infrastructure;
using ProjectManagement.Components;
using ProjectManagement.Components.Account;
using ProjectManagement.Components.Account.Pages;
using ProjectManagement.DependencyInjection;
using ProjectManagement.Server.HubsPM;
using ProjectManagement.Server.Middleware;
using ProjectManagement.Shared.Constant;
using Serilog;
using System.Globalization;
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
var TaskResourceBlueprintsDb = builder.Configuration.GetConnectionString("TaskResourceBlueprintsDb") ?? throw new InvalidOperationException("Connection string 'TaskResourceBlueprintsDb' not found.");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddCustomAuthentication(connectionString);
builder.Services.AddApplicationLayer();
builder.Services.AddPersistenceServices();
builder.Services.AddAuthPermissionsLayer();
builder.Services.AddTaskResourceBlueprints();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDbContextFactory<TaskResourceBlueprintsContext>(options =>
    options.UseSqlServer(TaskResourceBlueprintsDb, sqlOptions =>
    {
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));
builder.Services.AddProjectServices();
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
});
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Host.UseSerilog();

builder.Services.BlazorMHD();
var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
var supportedCultures = new[]
{
    new CultureInfo("en-US"),
    new CultureInfo("se-SE"),
};
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseRouting();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var tenantClaim = context.User.FindFirst(PMClaimsConst.Tentan)?.Value;
    var userIdClaim = context.User.FindFirst(PMClaimsConst.UserId)?.Value;
    await next();
});
app.UseAuthorization();
app.UseAntiforgery();
app.UseMiddleware<GlobalErrorHandling>();
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (ctx, http) =>
    {
        var userId = http.User.FindFirst("UserId")?.Value;
        if (userId is not null) ctx.Set("UserId", userId);

        var tenantId = http.User.FindFirst("tenant")?.Value;
        if (tenantId is not null) ctx.Set("TenantID", tenantId);
    };
});
app.MapStaticAssets();
app.MapControllers(); // API endpoints
app.MapHub<NotificationHub>("/notification");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(ProjectManagement.Client._Imports).Assembly
    );
// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
