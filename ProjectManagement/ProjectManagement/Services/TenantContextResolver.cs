using Microsoft.AspNetCore.Http;
using ProjectManagement.Shared.Constant;
using System.Security.Claims;

namespace ProjectManagement.Services;

public interface ITenantContextResolver
{
    Task EnsureResolvedAsync(CancellationToken ct = default);
}

public sealed class TenantContextResolver(
    TenantContext tenantContext,
    IHttpContextAccessor httpContextAccessor)
    : ITenantContextResolver
{
    public Task EnsureResolvedAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId > 0)
            return Task.CompletedTask;

        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        tenantContext.TenantId = GetIntClaim(user, PMClaimsConst.Tenant);
        tenantContext.UserId = GetNullableIntClaim(user, PMClaimsConst.UserId);
        tenantContext.DepartmentId = GetNullableIntClaim(user, PMClaimsConst.DepartmentId);

        return Task.CompletedTask;
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
