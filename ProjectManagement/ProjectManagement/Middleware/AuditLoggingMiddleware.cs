using ProjectManagement.Auditing;
using ProjectManagement.Services;
using ProjectManagement.Shared.Constant;
using System.Diagnostics;
using System.Security.Claims;

namespace ProjectManagement.Middleware;

public sealed class AuditLoggingMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    public async Task Invoke(HttpContext context, IAuditLogger auditLogger)
    {
        if (!ShouldAudit(context.Request))
        {
            await next(context);
            return;
        }

        var watch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            watch.Stop();
            var entry = BuildEntry(context, watch.ElapsedMilliseconds);
            await auditLogger.WriteAsync(entry, context.RequestAborted);
        }
    }

    private static bool ShouldAudit(HttpRequest request)
    {
        if (!MutatingMethods.Contains(request.Method))
            return false;

        var path = request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/client-logs", StringComparison.OrdinalIgnoreCase))
            return false;

        return path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/internal", StringComparison.OrdinalIgnoreCase);
    }

    private static AuditLogEntry BuildEntry(HttpContext context, long elapsedMilliseconds)
    {
        var tenantContext = context.RequestServices.GetService<TenantContext>();
        var user = context.User;
        var path = context.Request.Path.Value ?? string.Empty;

        return new AuditLogEntry(
            EventName: Classify(path, context.Request.Method),
            Action: $"{context.Request.Method.ToUpperInvariant()} {path}",
            Method: context.Request.Method,
            Path: path,
            StatusCode: context.Response.StatusCode,
            TraceId: Activity.Current?.Id ?? context.TraceIdentifier,
            UserId: ToStringOrNull(tenantContext?.UserId) ?? GetClaim(user, PMClaimsConst.UserId, ClaimTypes.NameIdentifier),
            TenantId: ToStringOrNull(tenantContext?.TenantId) ?? GetClaim(user, PMClaimsConst.Tenant),
            DepartmentId: ToStringOrNull(tenantContext?.DepartmentId) ?? GetClaim(user, PMClaimsConst.DepartmentId),
            RemoteIp: context.Connection.RemoteIpAddress?.ToString(),
            ElapsedMilliseconds: elapsedMilliseconds,
            TimestampUtc: DateTimeOffset.UtcNow);
    }

    private static string Classify(string path, string method)
    {
        if (path.StartsWith("/internal/tenants/reload", StringComparison.OrdinalIgnoreCase))
            return AuditEventNames.InternalTenantReload;

        if (path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/Identity/", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/Departments", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/Tenant", StringComparison.OrdinalIgnoreCase))
            return AuditEventNames.Identity;

        if (path.Contains("/ShareCalc", StringComparison.OrdinalIgnoreCase))
            return AuditEventNames.CalculationSharing;

        if (HttpMethods.IsDelete(method))
            return AuditEventNames.DataDeletion;

        return AuditEventNames.HttpMutation;
    }

    private static string? GetClaim(ClaimsPrincipal user, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static string? ToStringOrNull(int? value)
        => value.HasValue && value.Value > 0 ? value.Value.ToString() : null;
}
