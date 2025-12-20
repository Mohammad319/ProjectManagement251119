using Microsoft.AspNetCore.SignalR;
using ProjectManagement.Services;
using ProjectManagement.Shared.Constant;
using System.Security.Claims;

namespace ProjectManagement.SignalR;

/// <summary>
/// يعبّي TenantContext قبل تنفيذ أي Hub method.
/// ضروري لأن Hub invocations لا تعتمد على TenantContextMiddleware.
/// </summary>
public sealed class TenantContextHubFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var tenant = invocationContext.ServiceProvider.GetRequiredService<TenantContext>();
        PopulateFromClaims(invocationContext.Context.User, tenant);

        return await next(invocationContext);
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var tenant = context.ServiceProvider.GetRequiredService<TenantContext>();
        PopulateFromClaims(context.Context.User, tenant);

        await next(context);
    }

    private static void PopulateFromClaims(ClaimsPrincipal? user, TenantContext tenant)
    {
        if (tenant.TenantId > 0) return;
        if (user?.Identity?.IsAuthenticated != true) return;

        tenant.TenantId = GetIntClaim(user, PMClaimsConst.Tentan);
        tenant.UserId = GetNullableIntClaim(user, PMClaimsConst.UserId);
        tenant.DepartmentId = GetNullableIntClaim(user, PMClaimsConst.DepartmentId);
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
