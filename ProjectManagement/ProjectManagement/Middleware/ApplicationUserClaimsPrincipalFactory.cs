using AuthPermissions.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProjectManagement.Shared.Constant;
using System.Security.Claims;

namespace ProjectManagement.Middleware
{
    public class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
        : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, optionsAccessor)
    {
        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            if (user.TenantId.HasValue)
                identity.AddClaim(new Claim(PMClaimsConst.Tentan, user.TenantId.Value.ToString())); // ✅ tenant ثابت
            else
                identity.AddClaim(new Claim(PMClaimsConst.Tentan, "0")); // fallback آمن بدلاً من null

            identity.AddClaim(new Claim(PMClaimsConst.UserId, user.Id.ToString()));

            if (user.DepartmentId.HasValue)
                identity.AddClaim(new Claim(ClaimTypes.GroupSid, user.DepartmentId.Value.ToString()));

            if (!string.IsNullOrEmpty(user.Firstname) || !string.IsNullOrEmpty(user.Lastname))
                identity.AddClaim(new Claim(ClaimTypes.Surname, $"{user.Firstname} {user.Lastname}".Trim()));

            return identity;
        }
    }

}
