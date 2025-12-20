using ProjectManagement.Services;
using ProjectManagement.Shared.Constant;
using System.Security.Claims;

namespace ProjectManagement.Middleware;

/// <summary>
/// يعبّي TenantContext من Claims في بداية كل Request.
/// (أفضل من استخراج claims داخل DI، وأكثر ثباتاً مع Blazor Server/WASM)
/// </summary>
public sealed class TenantContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var user = context.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            tenantContext.TenantId = GetIntClaim(user, PMClaimsConst.Tentan);
            tenantContext.UserId = GetNullableIntClaim(user, PMClaimsConst.UserId);
            tenantContext.DepartmentId = GetNullableIntClaim(user, PMClaimsConst.DepartmentId);
        }

        // OPTIONAL: دعم header للـ integrations/WASM إذا لا يوجد claim
        // لا تعتمد عليه وحده بدون تحقق أمني إن كنت تسمح بتغييره من العميل.
 
        if (tenantContext.TenantId <= 0 &&
            context.Request.Headers.TryGetValue("X-Tenant-Id", out var tid) &&
            int.TryParse(tid.ToString(), out var parsedTid) &&
            parsedTid > 0)
        {
            tenantContext.TenantId = parsedTid;
        }

        await next(context);
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
