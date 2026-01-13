using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Middleware;
using ProjectManagement.Services;

namespace ProjectManagement.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseProjectManagementPipeline(this WebApplication app)
    {
        var isDev = app.Environment.IsDevelopment();

        app.UseResponseCompression();

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

        if (isDev)
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
        }

        app.UseGlobalExceptionHandling(isDev);

        app.UseHttpsRedirection();
        app.UseRouting();

        // API status code pages (problem+json)
        app.UseApiStatusCodePages();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapPost("/internal/tenants/reload", async (
            HttpRequest req,
            IConfiguration config,
            ITenantConnectionStringStore store) =>
                {
                    var secret = config["TenantReload:Secret"];
                    var header = req.Headers["X-Tenant-Reload-Secret"].ToString();

                    if (string.IsNullOrWhiteSpace(secret) || header != secret)
                        return Results.Unauthorized();

                    await store.ReloadAsync(req.HttpContext.RequestAborted);
                    return Results.Ok(new { status = "reloaded" });
                })
        .WithTags("Internal")
        .DisableAntiforgery();
        app.MapPost("/internal/tenants/reload/{tenantId:int}", async (
            int tenantId,
            HttpRequest req,
            IConfiguration config,
            ITenantConnectionStringStore store) =>
                {
                    var secret = config["TenantReload:Secret"];
                    var header = req.Headers["X-Tenant-Reload-Secret"].ToString();

                    if (string.IsNullOrWhiteSpace(secret) || header != secret)
                        return Results.Unauthorized();

                    await store.ReloadTenantAsync(tenantId, req.HttpContext.RequestAborted);
                    return Results.Ok(new { status = "reloaded", tenantId });
                })
        .WithTags("Internal")
        .DisableAntiforgery();

        // TenantContext after auth (depends on claims)
        app.UseMiddleware<TenantContextMiddleware>();

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
