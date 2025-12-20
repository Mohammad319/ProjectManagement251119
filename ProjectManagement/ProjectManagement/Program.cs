using Application;
using AuthPermissions;
using AuthPermissions.Context;
using BlazorMHD.UI.Core.Services;
using Domain.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Components;
using ProjectManagement.Components.Account;
using ProjectManagement.DependencyInjection;
using ProjectManagement.Middleware;
using ProjectManagement.Middleware.Identity;
using ProjectManagement.Server.Middleware;
using ProjectManagement.Shared.Constant;
using Serilog;
using TaskResourceBlueprints;
using TaskResourceBlueprints.Infrastructure;
using Persistence.Factory;
using ProjectManagement.SignalR;
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

var taskResourceBlueprintsDb = builder.Configuration.GetConnectionString("TaskResourceBlueprintsDb")
    ?? throw new InvalidOperationException("Connection string 'TaskResourceBlueprintsDb' not found.");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddCustomAuthentication(connectionString);

builder.Services.AddApplicationLayer();
builder.Services.AddPersistenceServices();
builder.Services.AddAuthPermissionsLayer();
builder.Services.AddTaskResourceBlueprints();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDbContextFactory<TaskResourceBlueprintsContext>(options =>
    options.UseSqlServer(taskResourceBlueprintsDb, sqlOptions =>
    {
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));

builder.Services.AddProjectServices();
builder.Services.AddSignalR(options =>
{
    // خيارات اختيارية
    options.EnableDetailedErrors = true;
});

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
});

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Host.UseSerilog();

builder.Services.BlazorMHD();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseResponseCompression();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ✅ مهم جداً: تعبئة TenantContext من claims لكل request
app.UseTenantContext();

app.UseAntiforgery();

app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"),
    apiApp => apiApp.UseMiddleware<GlobalErrorHandling>());

app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (ctx, http) =>
    {
        var userId = http.User.FindFirst(PMClaimsConst.UserId)?.Value;
        if (!string.IsNullOrWhiteSpace(userId)) ctx.Set(PMClaimsConst.UserId, userId);

        var tenantId = http.User.FindFirst(PMClaimsConst.Tentan)?.Value;
        if (!string.IsNullOrWhiteSpace(tenantId)) ctx.Set(PMClaimsConst.Tentan, tenantId);
    };
});

app.MapStaticAssets();
app.MapControllers();
app.MapHub<NotificationHub>("/notification");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ProjectManagement.Client._Imports).Assembly);

app.MapAdditionalIdentityEndpoints();

app.Run();
