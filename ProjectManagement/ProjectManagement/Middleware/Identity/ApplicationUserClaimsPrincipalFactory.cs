using AuthPermissions.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProjectManagement.Shared.Constant;
using System.Security.Claims;

namespace ProjectManagement.Middleware.Identity;

public sealed class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.TenantId is > 0)
            identity.AddClaim(new Claim(PMClaimsConst.Tentan, user.TenantId.Value.ToString()));

        if (user.UserId is > 0)
            identity.AddClaim(new Claim(PMClaimsConst.UserId, user.UserId.Value.ToString()));

        if (user.DepartmentId is > 0)
            identity.AddClaim(new Claim(PMClaimsConst.DepartmentId, user.DepartmentId.Value.ToString()));

        if (!string.IsNullOrWhiteSpace(user.Firstname) || !string.IsNullOrWhiteSpace(user.Lastname))
            identity.AddClaim(new Claim(PMClaimsConst.Full_name, $"{user.Firstname} {user.Lastname}".Trim()));

        return identity;
    }
}
