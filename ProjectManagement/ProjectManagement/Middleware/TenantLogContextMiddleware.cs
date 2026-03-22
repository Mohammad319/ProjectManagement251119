using ProjectManagement.Services;
using Serilog.Context;

namespace ProjectManagement.Middleware;

public sealed class TenantLogContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        using var traceScope = LogContext.PushProperty("TraceId", context.TraceIdentifier);
        using var tenantScope = LogContext.PushProperty("TenantID", tenantContext.TenantId > 0 ? tenantContext.TenantId : null);
        using var userScope = LogContext.PushProperty("UserId", tenantContext.UserId);
        using var departmentScope = LogContext.PushProperty("DepartmentId", tenantContext.DepartmentId);

        await next(context);
    }
}
