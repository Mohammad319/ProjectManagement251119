using Application;
using AuthPermissions;
using AuthPermissions.Context;
using BlazorMHD.UI.Core.Services;
using Domain.Settings;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Components;
using ProjectManagement.Components.Account;
using ProjectManagement.DependencyInjection;
using ProjectManagement.Middleware;
using ProjectManagement.Services;
using ProjectManagement.SignalR;
using Serilog;
using System.Diagnostics;
using TaskResourceBlueprints.Infrastructure;
using TaskResourceBlueprints;
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// -------------------------
// Settings + Identity helpers
// -------------------------
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

// -------------------------
// Connection strings
// -------------------------
var taskResourceBlueprintsDb = builder.Configuration.GetConnectionString("TaskResourceBlueprintsDb")
    ?? throw new InvalidOperationException("Connection string 'TaskResourceBlueprintsDb' not found.");

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// -------------------------
// Layers
// -------------------------
builder.Services.AddCustomAuthentication(defaultConnection);

builder.Services.AddApplicationLayer();
builder.Services.AddPersistenceServices();
builder.Services.AddAuthPermissionsLayer();
builder.Services.AddTaskResourceBlueprints();

// ✅ للتطوير فقط
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// -------------------------
// DbContextFactory (Blueprints)
// -------------------------
builder.Services.AddDbContextFactory<TaskResourceBlueprintsContext>(options =>
    options.UseSqlServer(taskResourceBlueprintsDb, sqlOptions =>
    {
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));

// -------------------------
// UI/tenant/services (server + wasm)
// -------------------------
builder.Services.AddProjectServices();

// -------------------------
// Response compression
// -------------------------
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
});

// -------------------------
// Logging
// -------------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// -------------------------
// Misc services
// -------------------------
builder.Services.BlazorMHD();
builder.Services.AddHttpContextAccessor();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        var traceId = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions["traceId"] = traceId;
    };
});

// Controllers (مرة واحدة فقط)
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var pd = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation error",
                Detail = "البيانات المرسلة غير صحيحة."
            };
            pd.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            return new BadRequestObjectResult(pd);
        };
    });

// UI helper
builder.Services.AddScoped<AppErrorDialog>();

// Middleware as IMiddleware
builder.Services.AddTransient<CorrelationIdMiddleware>();

var app = builder.Build();

var isDev = app.Environment.IsDevelopment();

// -------------------------
// Middleware pipeline
// -------------------------
app.UseResponseCompression();

app.UseStaticFiles();

// CorrelationId مبكر
app.UseMiddleware<CorrelationIdMiddleware>();

if (isDev)
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    // app.UseHsts();
}

// Global exception handler (API + UI)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = feature?.Error;

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                                           .CreateLogger("GlobalException");

        if (ex is not null)
            logger.LogError(ex, "Unhandled exception. TraceId={TraceId} Path={Path}", traceId, context.Request.Path);

        var isApi =
            context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers.Accept.Any(h =>
                h.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                h.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase));

        if (isApi)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var detail = isDev ? ex?.ToString() : "An unexpected error occurred.";

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 500,
                Title = "Server error",
                Detail = detail,
                Instance = context.Request.Path
            }.WithTraceId(traceId));

            return;
        }

        // UI redirect
        context.Response.Redirect($"/error?traceId={Uri.EscapeDataString(traceId)}");
    });
});

app.UseHttpsRedirection();

app.UseRouting();

// StatusCodePages للـ API فقط (404/401/403.. بدون HTML)
app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), apiApp =>
{
    apiApp.UseStatusCodePages(async statusCtx =>
    {
        var ctx = statusCtx.HttpContext;

        // إذا Controller كتب JSON بالفعل، لا تغطيه
        var contentType = ctx.Response.ContentType ?? "";
        if (contentType.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return;

        var traceId = Activity.Current?.Id ?? ctx.TraceIdentifier;

        ctx.Response.ContentType = "application/problem+json";

        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = ctx.Response.StatusCode,
            Title = "Request failed",
            Detail = $"HTTP {(int)ctx.Response.StatusCode}",
            Instance = ctx.Request.Path
        }.WithTraceId(traceId));
    });
});

app.UseAuthentication();
app.UseAuthorization();

// TenantContext بعد auth (لأن يعتمد على claims)
app.UseMiddleware<TenantContextMiddleware>();
app.UseAntiforgery();

// -------------------------
// Endpoints
// -------------------------
app.MapStaticAssets();

app.MapControllers();
app.MapHub<NotificationHub>("/notification");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ProjectManagement.Client._Imports).Assembly);

app.MapAdditionalIdentityEndpoints();

app.Run();

static class ProblemDetailsExt
{
    public static ProblemDetails WithTraceId(this ProblemDetails pd, string traceId)
    {
        pd.Extensions["traceId"] = traceId;
        return pd;
    }
}
