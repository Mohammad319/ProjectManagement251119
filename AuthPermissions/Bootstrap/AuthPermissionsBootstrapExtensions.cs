using AuthPermissions.Bootstrap;
using AuthPermissions.Context;
using AuthPermissions.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions;

public static class AuthPermissionsBootstrapExtensions
{
    public static async Task InitializeAuthPermissionsAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();

        var authDbContext = scope.ServiceProvider.GetRequiredService<AuthPermissionDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await AuthPermissionsMigrationBootstrapper.BaselineExistingSchemaAsync(authDbContext, app.Logger);
        await authDbContext.Database.MigrateAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await IdentityUserSyncHelper.EnsureRolesExistAsync(roleManager, IdentityUserSyncHelper.GetAllRoles()))
            throw new InvalidOperationException("Failed to initialize application roles.");

        var bootstrapEnabled = configuration.GetValue<bool>("Bootstrap:EnableConfiguredAdmin");
        if (!bootstrapEnabled)
            return;

        var email = configuration["User:Email"]?.Trim();
        var password = configuration["User:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var user = await userManager.FindByEmailAsync(email);
        var isNewUser = false;

        if (user is null)
        {
            user = new ApplicationUser();
            IdentityUserSyncHelper.ApplyToIdentityUser(
                user,
                email,
                email,
                tenantId: null,
                departmentId: null,
                localUserId: null,
                firstName: "Site",
                lastName: "Administrator",
                phoneNumber: null,
                phoneNumberConfirmed: false,
                lockoutEnabled: false,
                lockoutStart: null,
                lockoutEnd: null,
                isAppUser: true);

            user.EmailConfirmed = true;

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(" | ", createResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Failed to create configured site admin '{email}': {errors}");
            }

            isNewUser = true;
        }
        else
        {
            IdentityUserSyncHelper.ApplyToIdentityUser(
                user,
                email,
                email,
                tenantId: null,
                departmentId: null,
                localUserId: null,
                firstName: user.Firstname,
                lastName: user.Lastname,
                phoneNumber: user.PhoneNumber,
                phoneNumberConfirmed: user.PhoneNumberConfirmed,
                lockoutEnabled: false,
                lockoutStart: null,
                lockoutEnd: null,
                isAppUser: true);

            user.EmailConfirmed = true;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(" | ", updateResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Failed to update configured site admin '{email}': {errors}");
            }
        }

        if (!await IdentityUserSyncHelper.EnsureExactRolesAsync(
                userManager,
                user,
                IdentityUserSyncHelper.GetAppRoles(),
                IdentityUserSyncHelper.GetAllRoles()))
        {
            throw new InvalidOperationException($"Failed to assign required app roles to configured site admin '{email}'.");
        }

        if (!isNewUser)
            await userManager.UpdateSecurityStampAsync(user);
    }
}
