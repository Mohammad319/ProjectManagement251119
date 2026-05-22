using Microsoft.Extensions.Primitives;

namespace ProjectManagement.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            var cspReportOnly = configuration.GetValue("SecurityHeaders:CspReportOnly", true);

            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";

            if (cspReportOnly && !headers.ContainsKey("Content-Security-Policy-Report-Only"))
            {
                headers["Content-Security-Policy-Report-Only"] = new StringValues(
                    "default-src 'self'; " +
                    "base-uri 'self'; " +
                    "object-src 'none'; " +
                    "frame-ancestors 'self'; " +
                    "img-src 'self' data: blob:; " +
                    "font-src 'self' data:; " +
                    "style-src 'self' 'unsafe-inline'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                    "connect-src 'self' wss: https: http:;");
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
