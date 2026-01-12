namespace ProjectManagement.Middleware
{
    public class CorrelationIdMiddleware : IMiddleware
    {
        private const string HeaderName = "X-Correlation-ID";

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var cid = context.Request.Headers.TryGetValue(HeaderName, out var headerVal) && !string.IsNullOrWhiteSpace(headerVal)
                ? headerVal.ToString()
                : context.TraceIdentifier;

            context.TraceIdentifier = cid;

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = cid;
                return Task.CompletedTask;
            });

            using (Serilog.Context.LogContext.PushProperty("TraceId", cid))
            {
                await next(context);
            }
        }
    }
}
