using AuthPermissions.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Components.Account;
using ProjectManagement.Identity;
using ProjectManagement.Shared.Constant;
using System.Security.Claims;

namespace ProjectManagement.Extensions;

public static class AuthRegistration
{
    public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, string connectionString)
    {
        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();

        services.AddCascadingAuthenticationState();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        services.AddDbContextPool<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = ctx => HandleApiRedirectAsync(ctx, StatusCodes.Status401Unauthorized, "login"),
                OnRedirectToAccessDenied = ctx => HandleApiRedirectAsync(ctx, StatusCodes.Status403Forbidden, "access-denied")
            };
        });

        services.Configure<IdentityOptions>(options =>
        {
            options.User.RequireUniqueEmail = true;

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;

            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequiredUniqueChars = 4;
        });

        return services;
    }

    private static Task HandleApiRedirectAsync(
        RedirectContext<CookieAuthenticationOptions> context,
        int statusCode,
        string reason)
    {
        LogRedirect(context, statusCode, reason);

        if (IsApiRequest(context.Request))
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    private static bool IsApiRequest(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
            request.Path.StartsWithSegments("/notification", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var acceptsJson = request.Headers.Accept.Any(h =>
            !string.IsNullOrWhiteSpace(h) &&
            (h.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
             h.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase)));

        return acceptsJson ||
               string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    private static void LogRedirect(
        RedirectContext<CookieAuthenticationOptions> context,
        int statusCode,
        string reason)
    {
        var isAuthenticated = context.HttpContext.User.Identity?.IsAuthenticated == true;
        if (!isAuthenticated && !IsApiRequest(context.Request))
            return;

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ProjectManagement.Auth");

        var user = context.HttpContext.User;
        var userId = user.FindFirstValue(PMClaimsConst.UserId)
                     ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? "(anonymous)";

        logger.LogWarning(
            "Auth redirect triggered. Reason={Reason} StatusCode={StatusCode} Path={Path} UserId={UserId} Authenticated={Authenticated}",
            reason,
            statusCode,
            context.Request.Path,
            userId,
            isAuthenticated);
    }
}
