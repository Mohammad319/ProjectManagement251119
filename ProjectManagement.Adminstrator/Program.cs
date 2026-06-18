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
using ProjectManagement.Shared.Constant;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
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
builder.Services.AddDbContextFactory<AuthPermissionDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options => options.DetailedErrors = true);
builder.Services.AddHttpClient("SeqProxy")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false
    });

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "sv-SE", "en-US" };
    options.DefaultRequestCulture = new RequestCulture("sv-SE", "en-US");
    options.SupportedCultures = [.. supportedCultures.Select(CultureInfo.GetCultureInfo)];
    options.SupportedUICultures = [.. supportedCultures.Select(CultureInfo.GetCultureInfo)];
    options.ApplyCurrentCultureToResponseHeaders = true;
});

var adminLogConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration);

var adminDbConnStr = builder.Configuration.GetConnectionString("AuthPermissionsConnection");
if (!string.IsNullOrEmpty(adminDbConnStr))
{
    var colOpts = new ColumnOptions();
    colOpts.AdditionalColumns =
    [
        new SqlColumn { ColumnName = "TenantID", PropertyName = "TenantID", DataType = System.Data.SqlDbType.Int, AllowNull = true },
        new SqlColumn { ColumnName = "UserId",   PropertyName = "UserId",   DataType = System.Data.SqlDbType.NVarChar, DataLength = 450 }
    ];
    adminLogConfig = adminLogConfig.WriteTo.MSSqlServer(
        connectionString: adminDbConnStr,
        sinkOptions: new MSSqlServerSinkOptions { TableName = "Logs", SchemaName = "dbo", AutoCreateSqlTable = true },
        restrictedToMinimumLevel: LogEventLevel.Error,
        columnOptions: colOpts);
}

Log.Logger = adminLogConfig.CreateLogger();
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
app.MapSeqReverseProxy(PMRolesConst.APP.AdminManger);
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

await app.RunAsync();
