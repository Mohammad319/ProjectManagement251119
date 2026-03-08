using Microsoft.AspNetCore.Components.Authorization;
using ProjectManagement.Shared.Constant;
using System.Security.Claims;

namespace ProjectManagement.Services;

/// <summary>
/// Resolver يعبّي TenantContext من AuthenticationStateProvider داخل Blazor Server circuit.
/// </summary>
public interface ITenantContextResolver
{
    Task EnsureResolvedAsync(CancellationToken ct = default);
}

public sealed class TenantContextResolver(
    TenantContext tenantContext,
    AuthenticationStateProvider authStateProvider)
    : ITenantContextResolver
{
    public async Task EnsureResolvedAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId > 0)
            return;

        var state = await authStateProvider.GetAuthenticationStateAsync();
        var user = state.User;
        if (user?.Identity?.IsAuthenticated != true)
            return;

        tenantContext.TenantId = GetIntClaim(user, PMClaimsConst.Tenant);
        tenantContext.UserId = GetNullableIntClaim(user, PMClaimsConst.UserId);
        tenantContext.DepartmentId = GetNullableIntClaim(user, PMClaimsConst.DepartmentId);
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
