using Microsoft.AspNetCore.Components.Authorization;
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
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider authStateProvider)
    : ITenantContextResolver
{
    public async Task EnsureResolvedAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId > 0)
            return;

        var user = httpContextAccessor.HttpContext?.User;

        // Inside a Blazor Server circuit there is no HttpContext, so the claims must come from the
        // circuit's authentication state instead. Its ClaimsPrincipal carries the same tenant claims
        // that were issued at sign-in, so tenant-scoped queries work in interactive components too.
        if (user?.Identity?.IsAuthenticated != true)
        {
            user = await TryGetCircuitUserAsync().ConfigureAwait(false);
        }

        if (user?.Identity?.IsAuthenticated != true)
            return;

        tenantContext.TenantId = GetIntClaim(user, PMClaimsConst.Tenant);
        tenantContext.UserId = GetNullableIntClaim(user, PMClaimsConst.UserId);
        tenantContext.DepartmentId = GetNullableIntClaim(user, PMClaimsConst.DepartmentId);
    }

    private async Task<ClaimsPrincipal?> TryGetCircuitUserAsync()
    {
        try
        {
            var state = await authStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
            return state.User;
        }
        catch
        {
            // GetAuthenticationStateAsync can throw when called outside the rendering pipeline
            // (e.g. a plain API request). In that path HttpContext already provided the user, so
            // failing here simply means "could not resolve from the circuit" — leave unresolved.
            return null;
        }
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
