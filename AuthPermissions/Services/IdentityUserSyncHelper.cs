using AuthPermissions.Context;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using ProjectManagement.Shared.Constant;
using System.Security.Cryptography;

namespace AuthPermissions.Services;

public static class IdentityUserSyncHelper
{
    private static readonly string[] AppRoles =
    [
        PMRolesConst.APP.Admin,
        PMRolesConst.APP.Manger,
        PMRolesConst.APP.User
    ];

    private static readonly string[] TenantRoles =
    [
        PMRolesConst.Tenant.Admin,
        PMRolesConst.Tenant.Manger,
        PMRolesConst.Tenant.User,
        PMRolesConst.Tenant.Viewer
    ];

    private const string UpperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijkmnopqrstuvwxyz";
    private const string DigitChars = "23456789";
    private const string SpecialChars = "!@$?_-#%&*";

    public static IReadOnlyList<string> GetAppRoles() => AppRoles;

    public static IReadOnlyList<string> GetTenantRoles() => TenantRoles;

    public static IReadOnlyList<string> GetAllRoles() => [.. AppRoles, .. TenantRoles];

    public static string ResolveUserName(string? email, string? userName)
    {
        var normalizedUserName = userName?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedUserName))
            return normalizedUserName;

        return email?.Trim() ?? string.Empty;
    }

    public static string GenerateTemporaryPassword(int length = 16)
    {
        if (length < 8)
            length = 8;

        var chars = new List<char>(length)
        {
            GetRandomChar(UpperChars),
            GetRandomChar(LowerChars),
            GetRandomChar(DigitChars),
            GetRandomChar(SpecialChars)
        };

        var allChars = UpperChars + LowerChars + DigitChars + SpecialChars;
        while (chars.Count < length)
            chars.Add(GetRandomChar(allChars));

        for (var i = chars.Count - 1; i > 0; i--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[swapIndex]) = (chars[swapIndex], chars[i]);
        }

        return new string(chars.ToArray());
    }

    public static string GetDefaultRole(bool isAppUser)
        => isAppUser ? PMRolesConst.APP.User : PMRolesConst.Tenant.Admin;

    public static string? NormalizeRoleForUserScope(string? role, bool isAppUser, string? fallbackRole = null)
    {
        var requestedRole = role?.Trim();
        var allowedRoles = isAppUser ? AppRoles : TenantRoles;

        if (string.IsNullOrWhiteSpace(requestedRole))
            return ResolveFallbackRole(fallbackRole, isAppUser, allowedRoles);

        return allowedRoles.Contains(requestedRole, StringComparer.Ordinal)
            ? requestedRole
            : null;
    }

    public static void ApplyToIdentityUser(
        ApplicationUser target,
        string? email,
        string? userName,
        int? tenantId,
        int? departmentId,
        int? localUserId,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        bool phoneNumberConfirmed,
        bool lockoutEnabled,
        DateTimeOffset? lockoutStart,
        DateTimeOffset? lockoutEnd,
        bool isAppUser)
    {
        ArgumentNullException.ThrowIfNull(target);

        var normalizedEmail = email?.Trim() ?? string.Empty;
        var normalizedUserName = ResolveUserName(normalizedEmail, userName);

        target.Email = normalizedEmail;
        target.UserName = normalizedUserName;
        target.Firstname = firstName?.Trim() ?? string.Empty;
        target.Lastname = lastName?.Trim() ?? string.Empty;
        target.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        target.PhoneNumberConfirmed = phoneNumberConfirmed;
        target.LockoutEnabled = lockoutEnabled;
        target.LockoutStart = lockoutStart;
        target.LockoutEnd = lockoutEnd;
        target.TenantId = isAppUser ? null : tenantId;
        target.DepartmentId = isAppUser ? null : NormalizeNullableId(departmentId);
        target.UserId = isAppUser ? null : NormalizeNullableId(localUserId);
    }

    public static void ApplyToLocalUser(
        UserEntity target,
        string? email,
        string? userName,
        int? departmentId,
        string? firstName,
        string? lastName,
        string? externalAuthId)
    {
        ArgumentNullException.ThrowIfNull(target);

        var normalizedEmail = email?.Trim() ?? string.Empty;
        var normalizedUserName = ResolveUserName(normalizedEmail, userName);

        target.SetEmail(normalizedEmail);
        target.SetUserName(normalizedUserName);
        target.UpdateProfile(firstName, lastName, departmentId);
        target.SetExternalAuthId(externalAuthId);
    }

    public static async Task<bool> EnsureRolesExistAsync(RoleManager<IdentityRole> roleManager, IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(roleManager);
        ArgumentNullException.ThrowIfNull(roles);

        foreach (var role in roles.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal))
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
                return false;
        }

        return true;
    }

    public static async Task<bool> EnsureSingleRoleAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string? role)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(user);

        var normalizedRole = role?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedRole))
            return true;

        var currentRoles = await userManager.GetRolesAsync(user);

        if (currentRoles.Count == 1 && string.Equals(currentRoles[0], normalizedRole, StringComparison.Ordinal))
            return true;

        if (currentRoles.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
                return false;
        }

        var addResult = await userManager.AddToRoleAsync(user, normalizedRole);
        return addResult.Succeeded;
    }

    public static async Task<bool> EnsureExactRolesAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        IEnumerable<string> requiredRoles,
        IEnumerable<string>? removableRoles = null)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(requiredRoles);

        var required = requiredRoles
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var currentRoles = await userManager.GetRolesAsync(user);
        var removable = (removableRoles ?? currentRoles)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var rolesToRemove = currentRoles
            .Where(role => removable.Contains(role) && !required.Contains(role, StringComparer.Ordinal))
            .ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
                return false;
        }

        var rolesToAdd = required
            .Where(role => !currentRoles.Contains(role, StringComparer.Ordinal))
            .ToArray();

        if (rolesToAdd.Length > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
                return false;
        }

        return true;
    }

    private static string ResolveFallbackRole(string? fallbackRole, bool isAppUser, IReadOnlyCollection<string> allowedRoles)
    {
        var normalizedFallback = fallbackRole?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedFallback) && allowedRoles.Contains(normalizedFallback, StringComparer.Ordinal))
            return normalizedFallback;

        return GetDefaultRole(isAppUser);
    }

    private static int? NormalizeNullableId(int? value) => value.HasValue && value.Value > 0 ? value.Value : null;

    private static char GetRandomChar(string source)
        => source[RandomNumberGenerator.GetInt32(source.Length)];
}
