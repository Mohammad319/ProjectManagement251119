using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Configuration;
using ProjectManagement.Middleware;
using ProjectManagement.Security;
using ProjectManagement.Services;
using Serilog;
using System.Globalization;

namespace ProjectManagement.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseProjectManagementPipeline(this WebApplication app)
    {
        var isDev = app.Environment.IsDevelopment();

        app.UseResponseCompression();

        // Respect X-Forwarded-* headers when running behind reverse proxies (IIS/Nginx).
        app.UseForwardedHeaders();

        // Static files:
        // - In Development: disable caching so CSS/JS/images update immediately.
        // - In Production: keep aggressive caching for fingerprinted framework assets
        //   and shorter caching for the rest.
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (isDev)
                {
                    ctx.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "0";
                    return;
                }

                var path = ctx.Context.Request.Path;

                if (path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
                }
                else
                {
                    ctx.Context.Response.Headers.CacheControl = "public,max-age=604800";
                }
            }
        });

        // CorrelationId early
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
                diagnosticContext.Set("RemoteIp", httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            };
        });

        if (isDev)
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
        }

        app.UseGlobalExceptionHandling(isDev);

        if (!isDev)
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRequestLocalization();
        app.Use(async (context, next) =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");
            await next();
        });
        app.UseRouting();
        app.UseRateLimiter();

        // API status code pages (problem+json)
        app.UseApiStatusCodePages();

        app.UseAuthentication();
        app.UseAuthorization();

        var tenantReloadSecret = app.Configuration["TenantReload:Secret"];
        var tenantReloadEnabled = !string.IsNullOrWhiteSpace(tenantReloadSecret);

        if (tenantReloadEnabled)
        {
            app.MapPost("/internal/tenants/reload", async (
                HttpRequest req,
                ITenantConnectionStringStore store) =>
            {
                var header = req.Headers["X-Tenant-Reload-Secret"].ToString();

                if (!SecretComparison.FixedTimeEquals(header, tenantReloadSecret))
                    return Results.Unauthorized();

                await store.ReloadAsync(req.HttpContext.RequestAborted);
                return Results.Ok(new { status = "reloaded" });
            })
            .WithTags("Internal")
            .DisableAntiforgery();

            app.MapPost("/internal/tenants/reload/{tenantId:int}", async (
                int tenantId,
                HttpRequest req,
                ITenantConnectionStringStore store) =>
            {
                var header = req.Headers["X-Tenant-Reload-Secret"].ToString();

                if (!SecretComparison.FixedTimeEquals(header, tenantReloadSecret))
                    return Results.Unauthorized();

                await store.ReloadTenantAsync(tenantId, req.HttpContext.RequestAborted);
                return Results.Ok(new { status = "reloaded", tenantId });
            })
            .WithTags("Internal")
            .DisableAntiforgery();
        }

        // TenantContext after auth (depends on claims)
        app.UseMiddleware<TenantContextMiddleware>();
        app.UseMiddleware<AuditLoggingMiddleware>();

        app.UseAntiforgery();

        return app;
    }

    private static WebApplication UseApiStatusCodePages(this WebApplication app)
    {
        app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), apiApp =>
        {
            apiApp.UseStatusCodePages(async statusCtx =>
            {
                var ctx = statusCtx.HttpContext;

                var contentType = ctx.Response.ContentType ?? "";
                if (contentType.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase) ||
                    contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                    return;

                var traceId = System.Diagnostics.Activity.Current?.Id ?? ctx.TraceIdentifier;

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

        return app;
    }
}
