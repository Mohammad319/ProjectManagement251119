using Application;
using AuthPermissions;
using AuthPermissions.Context;
using BlazorMHD.UI.Core.Services;
using Domain.Settings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Persistence.Factory;
using ProjectManagement.Components.Account;
using ProjectManagement.Configuration;
using ProjectManagement.HealthChecks;
using ProjectManagement.Middleware;
using ProjectManagement.Monitoring;
using ProjectManagement.Services;
using Sentry.Extensibility;
using System.Diagnostics;
using System.IO;
using System.Threading.RateLimiting;
using TaskResourceBlueprints;
using TaskResourceBlueprints.Infrastructure;

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
        services.AddDbContextFactory<TaskResourceBlueprintsContext>(options =>
            options.UseSqlServer(conn.TaskResourceBlueprintsDb, sqlOptions =>
            {
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            }));

        // UI/tenant/services
        services.AddProjectServices();

        // Production hardening
        services.AddProductionHardening(builder, conn);

        // Response compression
        services.AddResponseCompression(opts =>
        {
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
        });

        // Misc
        services.AddBlazorMhdUI();
        services.AddHttpContextAccessor();
        services.AddTransient<ISentryEventProcessor, TenantSentryEventProcessor>();

        // Middleware registrations
        services.AddTransient<CorrelationIdMiddleware>();
        services.AddTransient<SecurityHeadersMiddleware>();
        services.AddTransient<TenantLogContextMiddleware>();

        // Controllers + model validation response
        services.AddProjectControllers();

        // UI helper
        services.AddScoped<AppErrorDialog>();

        return services;
    }

    private static IServiceCollection AddProductionHardening(
        this IServiceCollection services,
        WebApplicationBuilder builder,
        AppConnectionStrings conn)
    {
        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName("ProjectManagement");

        var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
        {
            Directory.CreateDirectory(dataProtectionKeysPath);
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
        }

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Application is running."))
            .AddCheck("auth-db", new SqlConnectionHealthCheck("PMTConnection", conn.DefaultConnection), tags: ["ready"])
            .AddCheck("task-resource-blueprints-db", new SqlConnectionHealthCheck("TaskResourceBlueprintsConnection", conn.TaskResourceBlueprintsDb), tags: ["ready"]);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, _) =>
            {
                var httpContext = context.HttpContext;
                var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

                httpContext.Response.ContentType = "application/problem+json";
                await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = "Too many requests were sent from this client. Please retry in a moment.",
                    Instance = httpContext.Request.Path
                }.WithTraceId(traceId));
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var path = httpContext.Request.Path;

                if (ShouldSkipRateLimiting(httpContext, path))
                    return RateLimitPartition.GetNoLimiter("skip-rate-limit");

                var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                if (path.StartsWithSegments("/Account/Login", StringComparison.OrdinalIgnoreCase))
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"{remoteIp}:login",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                }

                if (path.StartsWithSegments("/api/client-logs", StringComparison.OrdinalIgnoreCase))
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"{remoteIp}:client-logs",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"{remoteIp}:general",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 240,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    private static bool ShouldSkipRateLimiting(HttpContext httpContext, PathString path)
    {
        if (httpContext.WebSockets.IsWebSocketRequest)
            return true;

        if (path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/_content", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/notification", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/favicon", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var pathValue = path.Value;
        return !string.IsNullOrWhiteSpace(pathValue) && Path.HasExtension(pathValue);
    }
}
