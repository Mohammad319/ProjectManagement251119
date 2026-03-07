using AuthPermissions.Context;
using AuthPermissions.Services;
using Domain.DTO.User;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Services;

public sealed class TenantUserService(
    UserManager<ApplicationUser> userManager,
    IDbContextFactoryTenant dbFactory,
    ITenantContext currentTenant) : ITenantUserService
{

    private async Task<(ApplicationUser? User, bool CreatedNew)> EnsureAuthUserAsync(
        TenantUserDto tenantUser,
        string temporaryPassword)
    {
        if (tenantUser is null || string.IsNullOrWhiteSpace(tenantUser.Email))
            return (null, false);

        ApplicationUser? identityUser = null;

        if (!string.IsNullOrWhiteSpace(tenantUser.IdAuth))
            identityUser = await userManager.FindByIdAsync(tenantUser.IdAuth);

        identityUser ??= await userManager.FindByEmailAsync(tenantUser.Email);

        if (identityUser != null)
        {
            if (identityUser.TenantId != currentTenant.TenantId)
                return (null, false);

            IdentityUserSyncHelper.ApplyToIdentityUser(
                identityUser,
                tenantUser.Email,
                tenantUser.Username ?? tenantUser.Email,
                currentTenant.TenantId,
                tenantUser.DepartmentId,
                identityUser.UserId ?? (tenantUser.Id > 0 ? tenantUser.Id : null),
                tenantUser.Firstname,
                tenantUser.Lastname,
                tenantUser.PhoneNumber,
                tenantUser.PhoneNumberConfirmed,
                tenantUser.LockoutEnabled,
                tenantUser.LockoutStart,
                tenantUser.LockoutEnd,
                isAppUser: false);

            var updateExistingResult = await userManager.UpdateAsync(identityUser);
            if (!updateExistingResult.Succeeded)
                return (null, false);

            return (identityUser, false);
        }

        var newUser = new ApplicationUser
        {
            EmailConfirmed = true
        };

        IdentityUserSyncHelper.ApplyToIdentityUser(
            newUser,
            tenantUser.Email,
            tenantUser.Username ?? tenantUser.Email,
            currentTenant.TenantId,
            tenantUser.DepartmentId,
            tenantUser.Id > 0 ? tenantUser.Id : null,
            tenantUser.Firstname,
            tenantUser.Lastname,
            tenantUser.PhoneNumber,
            tenantUser.PhoneNumberConfirmed,
            tenantUser.LockoutEnabled,
            tenantUser.LockoutStart,
            tenantUser.LockoutEnd,
            isAppUser: false);

        var createResult = await userManager.CreateAsync(newUser, temporaryPassword);
        return createResult.Succeeded ? (newUser, true) : (null, false);
    }

    public async Task<bool> RegisterAsync(TenantUserDto request, CancellationToken ct = default)
    {
        if (request is null)
            return false;

        var normalizedRole = IdentityUserSyncHelper.NormalizeRoleForUserScope(request.Role, isAppUser: false);
        if (normalizedRole is null)
            return false;

        var password = IdentityUserSyncHelper.GenerateTemporaryPassword();
        var (identityUser, createdNew) = await EnsureAuthUserAsync(request, password);
        if (identityUser is null)
            return false;

        try
        {
            if (!await IdentityUserSyncHelper.EnsureSingleRoleAsync(userManager, identityUser, normalizedRole))
            {
                if (createdNew)
                    await userManager.DeleteAsync(identityUser);

                return false;
            }

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var localUser = request.Id > 0
                ? await context.User.FirstOrDefaultAsync(x => x.Id == request.Id, ct)
                : null;

            localUser ??= await context.User.FirstOrDefaultAsync(x => x.Email == request.Email, ct);

            if (localUser is null)
            {
                localUser = UserEntity.Create(
                    currentTenant.TenantId,
                    request.Email ?? string.Empty,
                    request.Username ?? request.Email ?? string.Empty,
                    request.DepartmentId,
                    request.Firstname,
                    request.Lastname,
                    identityUser.Id);

                context.User.Add(localUser);
            }
            else
            {
                IdentityUserSyncHelper.ApplyToLocalUser(
                    localUser,
                    request.Email,
                    request.Username ?? request.Email,
                    request.DepartmentId,
                    request.Firstname,
                    request.Lastname,
                    identityUser.Id);

                context.User.Update(localUser);
            }

            await context.SaveChangesAsync(ct);

            identityUser.UserId = localUser.Id;
            var syncResult = await userManager.UpdateAsync(identityUser);
            if (!syncResult.Succeeded)
            {
                if (createdNew)
                {
                    await userManager.DeleteAsync(identityUser);
                }

                return false;
            }

            await userManager.UpdateSecurityStampAsync(identityUser);
            return true;
        }
        catch
        {
            if (createdNew)
                await userManager.DeleteAsync(identityUser);

            return false;
        }
    }

    public async Task<bool> RecreateUserAsync(TenantUserDto tenantUser, CancellationToken ct = default)
    {
        if (tenantUser is null)
            return false;

        var normalizedRole = IdentityUserSyncHelper.NormalizeRoleForUserScope(tenantUser.Role, isAppUser: false);
        if (normalizedRole is null)
            return false;

        var password = IdentityUserSyncHelper.GenerateTemporaryPassword();
        var (authUser, createdNew) = await EnsureAuthUserAsync(tenantUser, password);
        if (authUser is null)
            return false;

        if (!await IdentityUserSyncHelper.EnsureSingleRoleAsync(userManager, authUser, normalizedRole))
        {
            if (createdNew)
                await userManager.DeleteAsync(authUser);

            return false;
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var userEntity = await context.User.FirstOrDefaultAsync(x => x.Id == tenantUser.Id, ct);
        if (userEntity == null)
            return false;

        IdentityUserSyncHelper.ApplyToLocalUser(
            userEntity,
            tenantUser.Email,
            tenantUser.Username ?? tenantUser.Email,
            tenantUser.DepartmentId,
            tenantUser.Firstname,
            tenantUser.Lastname,
            authUser.Id);

        context.User.Update(userEntity);
        await context.SaveChangesAsync(ct);

        authUser.UserId = userEntity.Id;
        var updateResult = await userManager.UpdateAsync(authUser);
        if (!updateResult.Succeeded)
            return false;

        await userManager.UpdateSecurityStampAsync(authUser);
        return true;
    }

    public async Task<bool> UpdateUserAsync(TenantUserDto user, CancellationToken ct = default)
    {
        if (user is null)
            return false;

        var normalizedRole = IdentityUserSyncHelper.NormalizeRoleForUserScope(user.Role, isAppUser: false);
        if (normalizedRole is null)
            return false;

        ApplicationUser? oldUser = null;
        if (!string.IsNullOrWhiteSpace(user.IdAuth))
            oldUser = await userManager.FindByIdAsync(user.IdAuth);

        oldUser ??= !string.IsNullOrWhiteSpace(user.Email)
            ? await userManager.FindByEmailAsync(user.Email)
            : null;

        if (oldUser is null || oldUser.TenantId != currentTenant.TenantId)
            return false;

        IdentityUserSyncHelper.ApplyToIdentityUser(
            oldUser,
            user.Email,
            user.Username ?? user.Email,
            currentTenant.TenantId,
            user.DepartmentId,
            oldUser.UserId ?? (user.Id > 0 ? user.Id : null),
            user.Firstname,
            user.Lastname,
            user.PhoneNumber,
            user.PhoneNumberConfirmed,
            user.LockoutEnabled,
            user.LockoutStart,
            user.LockoutEnd,
            isAppUser: false);

        var updateResult = await userManager.UpdateAsync(oldUser);
        if (!updateResult.Succeeded)
            return false;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var userEntity = await context.User.FirstOrDefaultAsync(x => x.Id == user.Id, ct);
        if (userEntity != null)
        {
            IdentityUserSyncHelper.ApplyToLocalUser(
                userEntity,
                user.Email,
                user.Username ?? user.Email,
                user.DepartmentId,
                user.Firstname,
                user.Lastname,
                oldUser.Id);

            context.User.Update(userEntity);
            await context.SaveChangesAsync(ct);
            oldUser.UserId = userEntity.Id;
            await userManager.UpdateAsync(oldUser);
        }

        if (!await IdentityUserSyncHelper.EnsureSingleRoleAsync(userManager, oldUser, normalizedRole))
            return false;

        await userManager.UpdateSecurityStampAsync(oldUser);
        return true;
    }

    private async Task<bool> DeleteUserFromAuthAsync(string authId)
    {
        var user = await userManager.FindByIdAsync(authId);
        if (user is null || user.TenantId != currentTenant.TenantId)
            return false;

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> RemoveAsync(string id, bool onlyfromregister, int userid, CancellationToken ct = default)
    {
        var deletedFromAuth = true;

        if (!string.IsNullOrWhiteSpace(id))
            deletedFromAuth = await DeleteUserFromAuthAsync(id);

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var u = await context.User.FirstOrDefaultAsync(x => x.ExternalAuthId == id || x.Id == userid, ct);
        if (u == null)
            return deletedFromAuth;

        if (onlyfromregister)
        {
            u.ClearExternalAuthId();
            context.User.Update(u);
            await context.SaveChangesAsync(ct);
            return deletedFromAuth;
        }

        if (!deletedFromAuth && !string.IsNullOrWhiteSpace(id))
            return false;

        await context.Calculations
            .Where(x => x.IsPrivate && x.CreatedBy == u.Id)
            .ExecuteDeleteAsync(ct);

        context.User.Remove(u);
        await context.SaveChangesAsync(ct);

        return true;
    }

    public async Task<List<TenantUserDto>> GetAllTenantUsersAsync(int? department, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var tenantUsersQuery = context.User.AsQueryable();
        if (department.HasValue)
            tenantUsersQuery = tenantUsersQuery.Where(x => x.DepartmentId == department.Value);

        var tenantUsers = await tenantUsersQuery
            .AsNoTracking()
            .ToListAsync(ct);

        var authDict = await userManager.Users
            .Where(x => x.TenantId == currentTenant.TenantId)
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.LockoutEnabled,
                x.LockoutEnd,
                x.LockoutStart,
                x.PhoneNumber,
                x.PhoneNumberConfirmed
            })
            .ToDictionaryAsync(x => x.Id, x => x, StringComparer.Ordinal, ct);

        var result = new List<TenantUserDto>(tenantUsers.Count);

        foreach (var tUser in tenantUsers)
        {
            authDict.TryGetValue(tUser.ExternalAuthId ?? string.Empty, out var appUser);

            result.Add(new TenantUserDto
            {
                Id = tUser.Id,
                Firstname = tUser.FirstName,
                Lastname = tUser.LastName,
                IdAuth = tUser.ExternalAuthId,
                DepartmentId = tUser.DepartmentId,
                Username = tUser.UserName,
                Email = tUser.Email,
                IsInAuth = appUser != null,
                LockoutEnabled = appUser?.LockoutEnabled ?? false,
                LockoutEnd = appUser?.LockoutEnd,
                LockoutStart = appUser?.LockoutStart,
                PhoneNumber = appUser?.PhoneNumber,
                PhoneNumberConfirmed = appUser?.PhoneNumberConfirmed ?? false
            });
        }

        return result;
    }
}
