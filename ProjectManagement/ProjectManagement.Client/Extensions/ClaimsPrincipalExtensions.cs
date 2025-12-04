using System.Security.Claims;

namespace ProjectManagement.Client.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsInAnyRole(this ClaimsPrincipal user, string rolesCsv)
        {
            if (user?.Identity?.IsAuthenticated != true) return false;
            var roles = rolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return roles.Any(user.IsInRole);
        }
    }

}
