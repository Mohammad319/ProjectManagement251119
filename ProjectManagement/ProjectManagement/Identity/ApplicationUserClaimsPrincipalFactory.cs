using AuthPermissions.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProjectManagement.Shared.Constant;
using System.Security.Claims;

namespace ProjectManagement.Identity;

public sealed class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(
        userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.TenantId > 0)
        {
            identity.AddClaim(new Claim(
                PMClaimsConst.Tenant,
                user.TenantId.Value.ToString()));
        }

        if (user.UserId > 0)
        {
            identity.AddClaim(new Claim(
                PMClaimsConst.UserId,
                user.UserId.Value.ToString()));
        }

        if (user.DepartmentId > 0)
        {
            identity.AddClaim(new Claim(
                PMClaimsConst.DepartmentId,
                user.DepartmentId.Value.ToString()));
        }

        var fullName = $"{user.Firstname} {user.Lastname}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            identity.AddClaim(new Claim(PMClaimsConst.FullName, fullName));
        }

        return identity;
    }
}
