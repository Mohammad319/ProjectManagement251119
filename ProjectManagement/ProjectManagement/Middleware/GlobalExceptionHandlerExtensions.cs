using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Extensions;

namespace ProjectManagement.Middleware;

public static class GlobalExceptionHandlerExtensions
{
    public static WebApplication UseGlobalExceptionHandling(this WebApplication app, bool isDev)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var ex = feature?.Error;

                var traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;

                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GlobalException");

                if (ex is not null)
                    logger.LogError(ex, "Unhandled exception. TraceId={TraceId} Path={Path}", traceId, context.Request.Path);

                var isApi =
                    context.Request.Path.StartsWithSegments("/api") ||
                    context.Request.Headers.Accept.Any(h =>
                        (!string.IsNullOrEmpty(h) && h.Contains("application/json", StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(h) && h.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase)));

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

                context.Response.Redirect($"/error?traceId={Uri.EscapeDataString(traceId)}");
            });
        });

        return app;
    }
}
