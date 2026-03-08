using Microsoft.Extensions.Primitives;
using ProjectManagement.Services;
using ProjectManagement.Shared.Constant;
using System.Net;
using System.Security.Claims;

namespace ProjectManagement.Middleware;

/// <summary>
/// يعبّي TenantContext من Claims في بداية كل Request.
/// (أفضل من استخراج claims داخل DI، وأكثر ثباتاً مع Blazor Server/WASM)
/// </summary>
public sealed class TenantContextMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<TenantContextMiddleware> logger)
{
    private readonly bool _allowHeaderOverride = configuration.GetValue<bool>("TenantContext:AllowHeaderOverride");
    private readonly string? _headerSecret = configuration["TenantContext:HeaderSecret"];

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var user = context.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            tenantContext.TenantId = GetIntClaim(user, PMClaimsConst.Tenant);
            tenantContext.UserId = GetNullableIntClaim(user, PMClaimsConst.UserId);
            tenantContext.DepartmentId = GetNullableIntClaim(user, PMClaimsConst.DepartmentId);
        }

        if (tenantContext.TenantId <= 0 &&
            _allowHeaderOverride &&
            TryGetTrustedTenantIdFromHeaders(context, out var headerTenantId))
        {
            tenantContext.TenantId = headerTenantId;
            logger.LogDebug("TenantContext populated from trusted header override for path {Path}.", context.Request.Path);
        }

        await next(context);
    }

    private bool TryGetTrustedTenantIdFromHeaders(HttpContext context, out int tenantId)
    {
        tenantId = 0;

        if (!context.Request.Headers.TryGetValue("X-Tenant-Id", out var tid) ||
            !int.TryParse(tid.ToString(), out tenantId) ||
            tenantId <= 0)
        {
            return false;
        }

        return IsTrustedOverrideRequest(context);
    }

    private bool IsTrustedOverrideRequest(HttpContext context)
    {
        if (!string.IsNullOrWhiteSpace(_headerSecret))
        {
            return context.Request.Headers.TryGetValue("X-Tenant-Secret", out StringValues suppliedSecret) &&
                   string.Equals(suppliedSecret.ToString(), _headerSecret, StringComparison.Ordinal);
        }

        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is null)
            return false;

        return IPAddress.IsLoopback(remoteIp) || Equals(remoteIp, context.Connection.LocalIpAddress);
    }

    private static int GetIntClaim(ClaimsPrincipal user, string claimType)
    {
        var value = user.FindFirst(claimType)?.Value;
        return (int.TryParse(value, out var parsed) && parsed > 0) ? parsed : 0;
    }

    private static int? GetNullableIntClaim(ClaimsPrincipal user, string claimType)
    {
        var value = user.FindFirst(claimType)?.Value;
        return (int.TryParse(value, out var parsed) && parsed > 0) ? parsed : null;
    }
}
