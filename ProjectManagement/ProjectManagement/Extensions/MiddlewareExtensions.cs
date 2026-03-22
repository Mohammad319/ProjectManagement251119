using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Middleware;
using ProjectManagement.Services;
using Serilog;
using Serilog.Events;

namespace ProjectManagement.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseProjectManagementPipeline(this WebApplication app)
    {
        var isDev = app.Environment.IsDevelopment();

        app.UseResponseCompression();

        // Respect X-Forwarded-* headers when running behind reverse proxies (IIS/Nginx).
        app.UseForwardedHeaders();

        // Static file caching (important for Blazor WASM startup):
        // - /_framework assets are fingerprinted -> cache aggressively
        // - other static assets get a shorter cache
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                var path = ctx.Context.Request.Path;

                if (path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase))
                {
                    // One year + immutable for fingerprinted files
                    ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
                }
                else
                {
                    // Reasonable default; tweak as you like
                    ctx.Context.Response.Headers.CacheControl = "public,max-age=604800"; // 7 days
                }
            }
        });

        // CorrelationId early
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();

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

                        if (header != tenantReloadSecret)
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

                        if (header != tenantReloadSecret)
                            return Results.Unauthorized();

                        await store.ReloadTenantAsync(tenantId, req.HttpContext.RequestAborted);
                        return Results.Ok(new { status = "reloaded", tenantId });
                    })
            .WithTags("Internal")
            .DisableAntiforgery();
        }

        // TenantContext after auth (depends on claims)
        app.UseMiddleware<TenantContextMiddleware>();
        app.UseMiddleware<TenantLogContextMiddleware>();

        // Request logging after auth/tenant resolution so logs contain user/tenant metadata.
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = (httpContext, _, ex) =>
            {
                if (ex is not null || httpContext.Response.StatusCode >= 500)
                    return LogEventLevel.Error;

                if (httpContext.Response.StatusCode >= 400)
                    return LogEventLevel.Warning;

                return LogEventLevel.Information;
            };

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                var tenantContext = httpContext.RequestServices.GetService<TenantContext>();

                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
                diagnosticContext.Set("RemoteIp", httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);

                if (tenantContext?.TenantId > 0)
                    diagnosticContext.Set("TenantID", tenantContext.TenantId);

                if (tenantContext?.UserId is > 0)
                    diagnosticContext.Set("UserId", tenantContext.UserId.Value);

                if (tenantContext?.DepartmentId is > 0)
                    diagnosticContext.Set("DepartmentId", tenantContext.DepartmentId.Value);
            };
        });

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

                // don't override if already JSON
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
