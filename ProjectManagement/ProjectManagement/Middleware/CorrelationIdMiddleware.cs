namespace ProjectManagement.Middleware
{
    using Serilog.Context;

    public class CorrelationIdMiddleware : IMiddleware
    {
        private const string HeaderName = "X-Correlation-ID";

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var cid) && !string.IsNullOrWhiteSpace(cid))
            {
                context.TraceIdentifier = cid.ToString();

                // اختياري: ضعه في LogContext لSerilog
                using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
                {
                    context.Response.Headers[HeaderName] = context.TraceIdentifier;
                    await next(context);
                    return;
                }
            }

            // لو ما في header
            context.Response.Headers[HeaderName] = context.TraceIdentifier;
            await next(context);
        }
    }

}
