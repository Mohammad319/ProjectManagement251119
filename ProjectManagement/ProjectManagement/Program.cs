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
using ProjectManagement.Shared.Constant;
using ProjectManagement.SignalR;
using Serilog;
using System.Diagnostics;
using TaskResourceBlueprints;
using TaskResourceBlueprints.Infrastructure;

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

// ✅ للتطوير فقط (إبقاءه كما لديك)
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDbContextFactory<TaskResourceBlueprintsContext>(options =>
    options.UseSqlServer(taskResourceBlueprintsDb, sqlOptions =>
    {
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));

builder.Services.AddProjectServices();

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
});

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.BlazorMHD();
builder.Services.AddHttpContextAccessor();

// ✅ ProblemDetails + traceId
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        var traceId = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions["traceId"] = traceId;
    };
});

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

// (إن كنت تحتاجه للـ UI فقط، اتركه)
builder.Services.AddScoped<AppErrorDialog>();

builder.Services.AddTransient<CorrelationIdMiddleware>();

var app = builder.Build();
var pathBase = app.Configuration["ASPNETCORE_PATHBASE"];
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}

var isDev = app.Environment.IsDevelopment();
app.UseStaticFiles();
// ✅ correlation id مبكر
app.UseMiddleware<CorrelationIdMiddleware>();

if (isDev)
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    // production HSTS optional
    // app.UseHsts();
}

// ✅ ExceptionHandler عام (API + UI) — بدل UseWhen/UseExceptionHandler
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

        var isApi = context.Request.Path.StartsWithSegments("/api") ||
                    context.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

        if (isApi)
        {
            var detail = isDev && ex is not null ? ex.Message : "حدث خطأ غير متوقع. حاول لاحقاً.";

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

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
        // context.Response.Redirect($"/error?traceId={Uri.EscapeDataString(traceId)}");
        var pb = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : "";
        context.Response.Redirect($"{pb}/error?traceId={Uri.EscapeDataString(traceId)}");

    });
});

app.UseResponseCompression();
app.UseHttpsRedirection();

app.UseRouting();

// ✅ StatusCodePages للـ API فقط (404/401/403.. بدون HTML)
app.UseStatusCodePages(async statusCtx =>
{
    var ctx = statusCtx.HttpContext;

    if (!ctx.Request.Path.StartsWithSegments("/api"))
        return;

    if (ctx.Response.HasStarted)
        return;

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
        Detail = ctx.Response.StatusCode switch
        {
            401 => "غير مصرح.",
            403 => "ليس لديك صلاحية.",
            404 => "المورد غير موجود.",
            _ => "تعذر إتمام الطلب."
        },
        Instance = ctx.Request.Path
    }.WithTraceId(traceId));
});

// ✅ Serilog request logging
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (ctx, http) =>
    {
        ctx.Set("TraceId", Activity.Current?.Id ?? http.TraceIdentifier);
        ctx.Set("Path", http.Request.Path.Value ?? "");
        ctx.Set("Method", http.Request.Method);

        var userId = http.User.FindFirst(PMClaimsConst.UserId)?.Value;
        if (!string.IsNullOrWhiteSpace(userId)) ctx.Set("UserId", userId);

        var tenantId = http.User.FindFirst(PMClaimsConst.Tenant)?.Value;
        if (!string.IsNullOrWhiteSpace(tenantId)) ctx.Set("TenantID", tenantId);
    };
});

app.UseAuthentication();
app.UseAuthorization();

app.UseTenantContext();
app.UseAntiforgery();

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
