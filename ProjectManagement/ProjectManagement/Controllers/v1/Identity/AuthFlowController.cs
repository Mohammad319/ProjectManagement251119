using AuthPermissions.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ProjectManagement.Controllers.v1.Identity
{
    [ApiController]
    public class AuthFlowController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthFlowController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }
        [Authorize]
        [HttpGet("/auth/refresh")]
        public async Task<IActionResult> Refresh(string? returnUrl = "/")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
                await _signInManager.RefreshSignInAsync(user); // هنا يُكتب الكوكي بشكل صحيح في طلب HTTP عادي

            return LocalRedirect(returnUrl ?? "/");
        }
        [Authorize]
        [HttpGet("/auth/refresh2")]
        public async Task<IActionResult> Refresh2(string? returnUrl = "/")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
                await _signInManager.RefreshSignInAsync(user);

            // تحقّق أنه محلي
            if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
                returnUrl = "/";

            return LocalRedirect(returnUrl); // آمن الآن
        }
    }

}
