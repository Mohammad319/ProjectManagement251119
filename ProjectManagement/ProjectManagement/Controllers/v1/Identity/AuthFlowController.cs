using AuthPermissions.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Helper;

namespace ProjectManagement.Controllers.v1.Identity;

[ApiController]
public class AuthFlowController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthFlowController> _logger;

    public AuthFlowController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AuthFlowController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("/auth/refresh")]
    public async Task<IActionResult> Refresh(string? returnUrl = "/")
    {
        var normalizedReturnUrl = NormalizeReturnUrl(returnUrl);
        var user = await RefreshCurrentUserAsync();

        if (user is not null)
        {
            _logger.LogInformation(
                "Authentication session refreshed for user {UserId}. ReturnUrl={ReturnUrl}",
                user.Id,
                normalizedReturnUrl);
        }

        return LocalRedirect(normalizedReturnUrl);
    }

    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("/auth/session/keep-alive")]
    public async Task<IActionResult> KeepAlive()
    {
        var user = await RefreshCurrentUserAsync();
        if (user is null)
        {
            _logger.LogWarning("Keep-alive was requested but no authenticated user was resolved.");
            return Unauthorized();
        }

        _logger.LogDebug("Authentication keep-alive refreshed for user {UserId}.", user.Id);
        return NoContent();
    }

    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("/auth/refresh2")]
    public Task<IActionResult> Refresh2(string? returnUrl = "/")
        => Refresh(returnUrl);

    private async Task<ApplicationUser?> RefreshCurrentUserAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return null;

        await _signInManager.RefreshSignInAsync(user);
        return user;
    }

    private string NormalizeReturnUrl(string? returnUrl)
    {
        var candidate = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl.Trim();
        if (!Url.IsLocalUrl(candidate))
            return "/";

        return AuthRecoveryPathHelper.NormalizeLocalUrl(candidate);
    }
}
