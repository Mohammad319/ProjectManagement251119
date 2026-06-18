using System.Security.Claims;
using AuthPermissions.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ProjectManagement.Services;

/// <summary>
/// A <see cref="SignInManager{T}"/> that also blocks sign-in for deactivated accounts
/// (<see cref="ApplicationUser.IsActive"/> == false). <see cref="CanSignInAsync"/> is the
/// single pre-sign-in gate used by password, passkey and refresh sign-in, so deactivation
/// is enforced on every path without duplicating checks in each login page.
/// </summary>
public sealed class ActiveCheckSignInManager(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<ApplicationUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation)
    : SignInManager<ApplicationUser>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    public override async Task<bool> CanSignInAsync(ApplicationUser user)
    {
        if (!user.IsActive)
            return false;

        return await base.CanSignInAsync(user);
    }
}
