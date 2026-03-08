using AuthPermissions.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Components.Account;
using ProjectManagement.Identity;

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
            options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            options.SlidingExpiration = true;
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = ctx => HandleApiRedirectAsync(ctx, StatusCodes.Status401Unauthorized),
                OnRedirectToAccessDenied = ctx => HandleApiRedirectAsync(ctx, StatusCodes.Status403Forbidden)
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

    private static Task HandleApiRedirectAsync(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
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
}
