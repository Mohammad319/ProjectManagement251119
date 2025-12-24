using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ProjectManagement.Middleware
{
    public sealed class GlobalExceptionMiddleware : IMiddleware
    {
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
            => _logger = logger;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

                _logger.LogError(ex,
                    "Unhandled exception. TraceId={TraceId} Path={Path} User={User}",
                    traceId,
                    context.Request.Path,
                    context.User?.Identity?.Name ?? "anon"
                );

                // إذا كان الطلب API أو يتوقع JSON
                if (WantsJson(context))
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/problem+json";

                    var problem = new ProblemDetails
                    {
                        Status = 500,
                        Title = "Server error",
                        Detail = "حدث خطأ غير متوقع. الرجاء المحاولة لاحقاً.",
                        Instance = context.Request.Path
                    };
                    problem.Extensions["traceId"] = traceId;

                    await context.Response.WriteAsJsonAsync(problem);
                    return;
                }

                // لو طلب صفحات/UI: توجيه لصفحة خطأ
                context.Response.Redirect($"/error?traceId={Uri.EscapeDataString(traceId)}");
            }
        }

        private static bool WantsJson(HttpContext ctx)
        {
            var accept = ctx.Request.Headers.Accept.ToString();
            var path = ctx.Request.Path.Value ?? "";
            return path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
                   || accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)
                   || ctx.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }

}
