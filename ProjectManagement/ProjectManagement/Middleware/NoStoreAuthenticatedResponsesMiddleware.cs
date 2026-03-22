namespace ProjectManagement.Middleware;

/// <summary>
/// Prevents browsers and intermediary caches from storing authenticated or account-related responses.
/// This reduces the chance of sensitive pages/data appearing from the back button or shared device caches.
/// Static files are not affected because UseStaticFiles runs earlier in the pipeline.
/// </summary>
public sealed class NoStoreAuthenticatedResponsesMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.OnStarting(() =>
        {
            if (ShouldDisableCaching(context))
            {
                var headers = context.Response.Headers;
                headers["Cache-Control"] = "no-store, no-cache, max-age=0";
                headers["Pragma"] = "no-cache";
                headers["Expires"] = "0";
            }

            return Task.CompletedTask;
        });

        await next(context);
    }

    private static bool ShouldDisableCaching(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
            return true;

        var path = context.Request.Path;
        return path.StartsWithSegments("/Account", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/Identity", StringComparison.OrdinalIgnoreCase);
    }
}
