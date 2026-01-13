using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Middleware;

namespace ProjectManagement.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseProjectManagementPipeline(this WebApplication app)
    {
        var isDev = app.Environment.IsDevelopment();

        app.UseResponseCompression();
        app.UseStaticFiles();

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

        // TenantContext after auth (depends on claims) :contentReference[oaicite:9]{index=9}
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
