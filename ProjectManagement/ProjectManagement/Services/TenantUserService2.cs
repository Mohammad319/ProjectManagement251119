using AuthPermissions.Context;
using Domain.DTO.User;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.Constant;
using System.Security.Cryptography;
using System.Text;

namespace ProjectManagement.Services;

public sealed class TenantUserService(
    UserManager<ApplicationUser> userManager,
    IDbContextFactoryTenant dbFactory,
    ITenantContext currentTenant) : ITenantUserService
{
    private static string GenerateRandomPassword(int length = 12)
    {
        const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var sb = new StringBuilder(length);

        for (int i = 0; i < length; i++)
            sb.Append(validChars[bytes[i] % validChars.Length]);

        return sb.ToString();
    }

    private async Task<ApplicationUser?> AuthRegisterUserAsync(TenantUserDto tenantUser, string temporaryPassword)
    {
        if (tenantUser is null) return null;

        var existingUser = await userManager.FindByEmailAsync(tenantUser.Email);
        if (existingUser != null) return existingUser;

        var newUser = new ApplicationUser
        {
            Email = tenantUser.Email,
            Firstname = tenantUser.Firstname,
            Lastname = tenantUser.Lastname,
            UserName = tenantUser.Email,
            TenantId = currentTenant.TenantId,
            DepartmentId = tenantUser.DepartmentId,
            LockoutEnabled = tenantUser.LockoutEnabled,
            LockoutStart = tenantUser.LockoutStart,
            LockoutEnd = tenantUser.LockoutEnd,
            PhoneNumber = tenantUser.PhoneNumber,
            PhoneNumberConfirmed = tenantUser.PhoneNumberConfirmed,
            UserId = tenantUser.Id
        };

        var result = await userManager.CreateAsync(newUser, temporaryPassword);
        return result.Succeeded ? newUser : null;
    }

    public async Task<bool> RegisterAsync(TenantUserDto request, CancellationToken ct = default)
    {
        if (request is null) return false;
        if (string.IsNullOrEmpty(request.Role)) request.Role = PMRolesConst.Tenant.Admin;

        // ✅ كلمة مرور مؤقتة آمنة (لا تجعلها email)
        var password = GenerateRandomPassword();

        var identityUser = await AuthRegisterUserAsync(request, password);
        if (identityUser is null) return false;

        try
        {
            await userManager.AddToRoleAsync(identityUser, request.Role);
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var localUser = await context.User
                .FirstOrDefaultAsync(x => x.Email == request.Email);

            if (localUser is null)
            {
                localUser = new UserEntity
                {
                    ExternalAuthId = identityUser.Id,
                    FirstName = request.Firstname,
                    LastName = request.Lastname,
                    Email = request.Email,
                    TenantId = currentTenant.TenantId,
                    DepartmentId = request.DepartmentId,
                    UserName = request.Email,
                };

                context.User.Add(localUser);
                await context.SaveChangesAsync();

                identityUser.UserId = localUser.Id;
                await userManager.UpdateAsync(identityUser);
            }

            return true;
        }
        catch
        {
            // rollback على مستوى auth user
            await userManager.DeleteAsync(identityUser);
            return false;
        }
    }

    public async Task<bool> RecreateUserAsync(TenantUserDto tenantUser, CancellationToken ct = default)
    {
        if (tenantUser is null) return false;
        if (string.IsNullOrEmpty(tenantUser.Role)) tenantUser.Role = PMRolesConst.Tenant.Admin;

        var password = GenerateRandomPassword();

        var authUser = await AuthRegisterUserAsync(tenantUser, password);
        if (authUser is null) return false;

        await userManager.AddToRoleAsync(authUser, tenantUser.Role);
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var userEntity = await context.User.FirstOrDefaultAsync(x => x.Id == tenantUser.Id);
        if (userEntity != null)
        {
            userEntity.ExternalAuthId = authUser.Id;
            context.User.Update(userEntity);
            await context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> UpdateUserAsync(TenantUserDto user, CancellationToken ct = default)
    {
        if (user is null) return false;

        ApplicationUser? oldUser = null;
        if (!string.IsNullOrWhiteSpace(user.IdAuth))
            oldUser = await userManager.FindByIdAsync(user.IdAuth);

        if (oldUser is null || oldUser.TenantId != currentTenant.TenantId)
            return false;

        oldUser.Firstname = user.Firstname;
        oldUser.Lastname = user.Lastname;
        oldUser.PhoneNumber = user.PhoneNumber;
        oldUser.PhoneNumberConfirmed = user.PhoneNumberConfirmed;
        oldUser.LockoutEnabled = user.LockoutEnabled;
        oldUser.LockoutStart = user.LockoutStart;
        oldUser.LockoutEnd = user.LockoutEnd;

        await userManager.UpdateAsync(oldUser);
        await userManager.UpdateSecurityStampAsync(oldUser);
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var userEntity = await context.User.FirstOrDefaultAsync(x => x.Id == user.Id);
        if (userEntity != null)
        {
            userEntity.DepartmentId = user.DepartmentId;
            userEntity.FirstName = user.Firstname;
            userEntity.LastName = user.Lastname;
            userEntity.ExternalAuthId = user.IdAuth;

            context.User.Update(userEntity);
            await context.SaveChangesAsync();
        }

        var roles = await userManager.GetRolesAsync(oldUser);
        if (!roles.Contains(user.Role))
        {
            await userManager.RemoveFromRolesAsync(oldUser, roles);
            await userManager.AddToRoleAsync(oldUser, user.Role);
        }

        return true;
    }

    private async Task<bool> DeleteUserFromAuthAsync(string authId)
    {
        var user = await userManager.FindByIdAsync(authId);
        if (user is null || user.TenantId != currentTenant.TenantId) return false;

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> RemoveAsync(string id, bool onlyfromregister, int userid, CancellationToken ct = default)
    {
        var deletedFromAuth = true;

        if (!string.IsNullOrWhiteSpace(id))
            deletedFromAuth = await DeleteUserFromAuthAsync(id);
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var u = await context.User.FirstOrDefaultAsync(x => x.ExternalAuthId == id || x.Id == userid);

        if (!onlyfromregister && u != null && deletedFromAuth)
        {
            var calcs = context.Calculations.Where(x => x.IsPrivate && x.CreatedBy == u.Id);
            context.Calculations.RemoveRange(calcs);

            context.User.Remove(u);
            await context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<List<TenantUserDto>> GetAllTenantUsersAsync(int? department, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var tenantUsersQuery = context.User.AsQueryable();

        if (department.HasValue)
            tenantUsersQuery = tenantUsersQuery.Where(x => x.DepartmentId == department.Value);

        var tenantUsers = await tenantUsersQuery.AsNoTracking().ToListAsync();

        var authUsers = await userManager.Users
            .Where(x => x.TenantId == currentTenant.TenantId)
            .AsNoTracking()
            .ToListAsync();

        var authDict = authUsers.ToDictionary(x => x.Id, x => x);

        return tenantUsers.Select(tUser =>
        {
            authDict.TryGetValue(tUser.ExternalAuthId ?? "", out var appUser);

            return new TenantUserDto
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
                LockoutStart = appUser?.LockoutStart
            };
        }).ToList();
    }
}
