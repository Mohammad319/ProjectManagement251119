using Application;
using Application.Feature.PriceImport;
using AuthPermissions;
using AuthPermissions.Context;
using BlazorMHD.UI.Core.Services;
using Domain.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Components.Account;
using ProjectManagement.Auditing;
using ProjectManagement.Configuration;
using ProjectManagement.Middleware;
using ProjectManagement.Services;
using TaskResourceBlueprints;
using TaskResourceBlueprints.Infrastructure;
using Persistence.Factory;
using System.IO;
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace ProjectManagement.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectManagementApp(
        this IServiceCollection services,
        WebApplicationBuilder builder,
        AppConnectionStrings conn)
    {
        // Settings + Identity helpers
        services.AddScoped<LocalizedIdentityEmailSender>();
        services.AddScoped<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<LocalizedIdentityEmailSender>());
        services.AddScoped<IAccountNotificationEmailSender>(sp => sp.GetRequiredService<LocalizedIdentityEmailSender>());
        services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

        // Layers
        services.AddCustomAuthentication(conn.DefaultConnection);
        services.AddApplicationLayer();
        services.AddPersistenceServices();
        services.AddAuthPermissionsLayer();
        services.AddTaskResourceBlueprints();

        // Dev only
        services.AddDatabaseDeveloperPageExceptionFilter();

        // DbContextFactory (Blueprints)
        services.AddDbContext<TaskResourceBlueprintsContext>(options =>
            options.UseSqlServer(conn.TaskResourceBlueprintsDb, sqlOptions =>
            {
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                sqlOptions.EnableRetryOnFailure();
            }));
        services.AddScoped<IDbContextFactory<TaskResourceBlueprintsContext>, TenantAwareTaskResourceBlueprintsContextFactory>();

        // UI/tenant/services
        services.AddProjectServices();
        services.Configure<PriceImportStorageOptions>(builder.Configuration.GetSection("PriceImport"));
        services.PostConfigure<PriceImportStorageOptions>(options =>
        {
            var configuredRoot = !string.IsNullOrWhiteSpace(options.StorageRoot)
                ? options.StorageRoot
                : options.RootPath;

            options.RootPath = Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(builder.Environment.ContentRootPath, configuredRoot);

            if (options.MaxUploadSizeMb <= 0)
                options.MaxUploadSizeMb = 20;

            options.MaxFileSizeBytes = options.MaxUploadSizeMb * 1024L * 1024L;

            if (options.AllowedExtensions.Length == 0)
                options.AllowedExtensions = [".xlsx", ".xls", ".docx", ".pdf"];
        });
        services.Configure<PriceImportAiOptions>(builder.Configuration.GetSection("PriceImportAi"));
        services.AddSingleton<IPriceImportExtractionClient, ExcelPriceImportExtractionClient>();
        services.AddHttpClient<IPriceImportAiExtractionService, PriceImportAiExtractionService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[] { "sv-SE", "en-US" };
            options.DefaultRequestCulture = new RequestCulture("sv-SE", "en-US");
            options.SupportedCultures = [.. supportedCultures.Select(CultureInfo.GetCultureInfo)];
            options.SupportedUICultures = [.. supportedCultures.Select(CultureInfo.GetCultureInfo)];
            options.ApplyCurrentCultureToResponseHeaders = true;

            // Default to Swedish. Only an explicit user choice (query string or the culture cookie set
            // by the language toggle) switches away from it — the browser's Accept-Language header must
            // NOT silently force English, otherwise SSR + interactive-server pages render in English.
            options.RequestCultureProviders =
            [
                new QueryStringRequestCultureProvider(),
                new CookieRequestCultureProvider()
            ];
        });

        // Response compression
        services.AddResponseCompression(opts =>
        {
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
        });

        // Misc
        services.AddBlazorMhdUI(o => o.DefaultToastSeconds = 6);
        services.AddHttpContextAccessor();

        // Data Protection keys should be persisted in production so auth cookies remain valid across restarts.
        var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
        {
            Directory.CreateDirectory(dataProtectionKeysPath);

            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
                .SetApplicationName("ProjectManagement");
        }

        // Built-in rate limiting to protect public and noisy endpoints.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var path = httpContext.Request.Path.Value ?? string.Empty;
                var isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? httpContext.User.FindFirst("UserId")?.Value
                             ?? remoteIp;

                if (path.StartsWith("/api/client-logs", StringComparison.OrdinalIgnoreCase))
                {
                    return RateLimitPartition.GetFixedWindowLimiter($"client-logs:{remoteIp}", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
                }

                if (path.StartsWith("/Account/Manage", StringComparison.OrdinalIgnoreCase))
                {
                    var managePartition = isAuthenticated ? $"account-manage:user:{userId}" : $"account-manage:ip:{remoteIp}";
                    return RateLimitPartition.GetFixedWindowLimiter(managePartition, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
                }

                if (path.Equals("/Account/PasskeyCreationOptions", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("/Account/PasskeyRequestOptions", StringComparison.OrdinalIgnoreCase))
                {
                    var passkeyPartition = isAuthenticated ? $"passkey:user:{userId}" : $"passkey:ip:{remoteIp}";
                    return RateLimitPartition.GetFixedWindowLimiter(passkeyPartition, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
                }

                if (path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/auth", StringComparison.OrdinalIgnoreCase))
                {
                    return RateLimitPartition.GetFixedWindowLimiter($"auth:{remoteIp}", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 12,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
                }

                return RateLimitPartition.GetFixedWindowLimiter($"global:{remoteIp}", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        });

        // Middleware registrations
        services.AddSingleton<IAuditLogger, SerilogAuditLogger>();
        services.AddTransient<CorrelationIdMiddleware>();
        //services.AddTransient<SecurityHeadersMiddleware>();

        // Controllers + model validation response
        services.AddProjectControllers();

        // UI helper
        services.AddScoped<AppErrorDialog>();

        return services;
    }
}
