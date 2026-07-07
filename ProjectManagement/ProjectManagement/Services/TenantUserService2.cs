using AuthPermissions.Context;
using AuthPermissions.Services;
using Domain.DTO.User;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Components.Account;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Services.UI;

namespace ProjectManagement.Services;

public sealed class TenantUserService(
    UserManager<ApplicationUser> userManager,
    IDbContextFactoryTenant dbFactory,
    ITenantContext currentTenant,
    IAccountNotificationEmailSender accountNotificationEmailSender,
    IUserManagementAuditService auditService,
    ILogger<TenantUserService> logger) : ITenantUserService
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

        NormalizeDepartmentSelection(request, normalizedRole);

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

            await SyncUserDepartmentAccessesAsync(context, localUser, request.DepartmentIds, ct);
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

            if (createdNew)
            {
                try
                {
                    await accountNotificationEmailSender.SendUserCreatedAsync(
                        identityUser.Email ?? request.Email ?? string.Empty,
                        $"{request.Firstname} {request.Lastname}".Trim(),
                        password,
                        passwordWasGenerated: true,
                        ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Tenant user {Email} was created but the notification email could not be sent.", identityUser.Email ?? request.Email);
                }
            }

            logger.LogInformation(
                "Tenant user registered. TenantId={TenantId} LocalUserId={LocalUserId} AuthUserId={AuthUserId} Role={Role} CreatedNew={CreatedNew}",
                currentTenant.TenantId, localUser.Id, identityUser.Id, normalizedRole, createdNew);
            await auditService.WriteAsync(
                createdNew ? "user.created" : "user.registered",
                localUser.Id,
                identityUser.Id,
                DisplayName(request.Firstname, request.Lastname, request.Email),
                $"Roll: {TenantRoleLabelSv(normalizedRole)}; Avdelningar: {await DepartmentNamesTextAsync(context, request.DepartmentIds, ct)}",
                ct: ct);
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

        NormalizeDepartmentSelection(tenantUser, normalizedRole);

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

        await SyncUserDepartmentAccessesAsync(context, userEntity, tenantUser.DepartmentIds, ct);
        await context.SaveChangesAsync(ct);

        authUser.UserId = userEntity.Id;
        var updateResult = await userManager.UpdateAsync(authUser);
        if (!updateResult.Succeeded)
            return false;

        await userManager.UpdateSecurityStampAsync(authUser);
        await auditService.WriteAsync(
            "user.auth-recreated",
            userEntity.Id,
            authUser.Id,
            DisplayName(tenantUser.Firstname, tenantUser.Lastname, tenantUser.Email),
            $"Roll: {TenantRoleLabelSv(normalizedRole)}",
            ct: ct);
        return true;
    }

    public async Task<bool> UpdateUserAsync(TenantUserDto user, CancellationToken ct = default)
    {
        if (user is null)
            return false;

        var normalizedRole = IdentityUserSyncHelper.NormalizeRoleForUserScope(user.Role, isAppUser: false);
        if (normalizedRole is null)
            return false;

        NormalizeDepartmentSelection(user, normalizedRole);

        ApplicationUser? oldUser = null;
        if (!string.IsNullOrWhiteSpace(user.IdAuth))
            oldUser = await userManager.FindByIdAsync(user.IdAuth);

        oldUser ??= !string.IsNullOrWhiteSpace(user.Email)
            ? await userManager.FindByEmailAsync(user.Email)
            : null;

        if (oldUser is null || oldUser.TenantId != currentTenant.TenantId)
            return false;

        // Block demoting the last administrator out of the Admin role.
        if (normalizedRole != PMRolesConst.Tenant.Admin && await IsLastTenantAdminAsync(oldUser))
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
        var previousDepartmentIds = userEntity is null
            ? new List<int>()
            : await GetUserDepartmentIdsAsync(context, userEntity.Id, userEntity.DepartmentId, ct);
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
            await SyncUserDepartmentAccessesAsync(context, userEntity, user.DepartmentIds, ct);
            await context.SaveChangesAsync(ct);
            oldUser.UserId = userEntity.Id;
            await userManager.UpdateAsync(oldUser);
        }

        if (!await IdentityUserSyncHelper.EnsureSingleRoleAsync(userManager, oldUser, normalizedRole))
            return false;

        await userManager.UpdateSecurityStampAsync(oldUser);

        var departmentPart = await DepartmentChangeTextAsync(context, previousDepartmentIds, user.DepartmentIds, ct);

        await auditService.WriteAsync(
            "user.updated",
            user.Id,
            oldUser.Id,
            DisplayName(user.Firstname, user.Lastname, user.Email),
            $"Roll: {TenantRoleLabelSv(normalizedRole)}; {departmentPart}",
            ct: ct);
        return true;
    }

    private static void NormalizeDepartmentSelection(TenantUserDto user, string normalizedRole)
    {
        var selected = user.DepartmentIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (selected.Count == 0 && user.DepartmentId is > 0)
            selected.Add(user.DepartmentId.Value);

        user.DepartmentIds = selected;
        user.DepartmentId = normalizedRole == PMRolesConst.Tenant.Admin
            ? null
            : selected.FirstOrDefault() is var first && first > 0 ? first : null;
    }

    private static async Task<List<int>> GetUserDepartmentIdsAsync(
        Persistence.Context.ShardingSingleDbContext context,
        int userId,
        int? primaryDepartmentId,
        CancellationToken ct)
    {
        var ids = await context.UserDepartmentAccesses
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DepartmentId)
            .Select(x => x.DepartmentId)
            .ToListAsync(ct);

        if (ids.Count == 0 && primaryDepartmentId is > 0)
            ids.Add(primaryDepartmentId.Value);

        return ids;
    }

    private static async Task SyncUserDepartmentAccessesAsync(
        Persistence.Context.ShardingSingleDbContext context,
        UserEntity user,
        IReadOnlyCollection<int> selectedDepartmentIds,
        CancellationToken ct)
    {
        var selected = selectedDepartmentIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var existing = await context.UserDepartmentAccesses
            .Where(x => x.UserId == user.Id)
            .ToListAsync(ct);

        foreach (var access in existing.Where(x => !selected.Contains(x.DepartmentId)).ToList())
            context.UserDepartmentAccesses.Remove(access);

        for (var index = 0; index < selected.Count; index++)
        {
            var departmentId = selected[index];
            var isPrimary = index == 0;
            var access = existing.FirstOrDefault(x => x.DepartmentId == departmentId);
            if (access is null)
            {
                context.UserDepartmentAccesses.Add(UserDepartmentAccessEntity.Create(user.Id, departmentId, isPrimary));
                continue;
            }

            access.SetPrimary(isPrimary);
        }
    }

    private static async Task<string> DepartmentChangeTextAsync(
        Persistence.Context.ShardingSingleDbContext context,
        IReadOnlyCollection<int> previousDepartmentIds,
        IReadOnlyCollection<int> newDepartmentIds,
        CancellationToken ct)
    {
        var previous = previousDepartmentIds.Where(id => id > 0).Distinct().ToList();
        var current = newDepartmentIds.Where(id => id > 0).Distinct().ToList();

        if (previous.SequenceEqual(current))
            return $"Avdelningar: {await DepartmentNamesTextAsync(context, current, ct)}";

        return $"Avdelningar ändrade: {await DepartmentNamesTextAsync(context, previous, ct)} → {await DepartmentNamesTextAsync(context, current, ct)}";
    }

    private static async Task<string> DepartmentNamesTextAsync(
        Persistence.Context.ShardingSingleDbContext context,
        IReadOnlyCollection<int> departmentIds,
        CancellationToken ct)
    {
        var ids = departmentIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
            return "Ingen avdelning";

        var namesById = await context.Department
            .AsNoTracking()
            .Where(d => ids.Contains(d.Id))
            .Select(d => new { d.Id, d.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

        var names = ids
            .Select(id => namesById.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name) ? name : "Okänd avdelning")
            .ToList();

        return string.Join(", ", names);
    }

    /// <summary>Resolves a department id to its display name (Swedish "Ingen avdelning" when unset).</summary>
    private static async Task<string> DepartmentNameAsync(
        Persistence.Context.ShardingSingleDbContext context, int? departmentId, CancellationToken ct)
    {
        if (!departmentId.HasValue)
            return "Ingen avdelning";

        var name = await context.Department
            .AsNoTracking()
            .Where(d => d.Id == departmentId.Value)
            .Select(d => d.Name)
            .FirstOrDefaultAsync(ct);

        return string.IsNullOrWhiteSpace(name) ? "Ingen avdelning" : name;
    }

    /// <summary>Swedish label for a tenant role value, matching the UI's permission labels.</summary>
    private static string TenantRoleLabelSv(string? role)
        => string.IsNullOrWhiteSpace(role) ? "—" : UI.PermissionDisplay.Label(role);

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
        // Deleting or unregistering the last admin would strip the tenant of admin access.
        if (!string.IsNullOrWhiteSpace(id))
        {
            var authUser = await userManager.FindByIdAsync(id);
            if (authUser is not null
                && authUser.TenantId == currentTenant.TenantId
                && await IsLastTenantAdminAsync(authUser))
                return false;
        }

        var deletedFromAuth = true;

        if (!string.IsNullOrWhiteSpace(id))
            deletedFromAuth = await DeleteUserFromAuthAsync(id);

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var u = await context.User.FirstOrDefaultAsync(x => x.ExternalAuthId == id || x.Id == userid, ct);
        if (u == null)
            return deletedFromAuth;

        if (onlyfromregister)
        {
            var displayName = DisplayName(u.FirstName, u.LastName, u.Email);
            u.ClearExternalAuthId();
            context.User.Update(u);
            await context.SaveChangesAsync(ct);
            logger.LogInformation(
                "Tenant user unregistered. TenantId={TenantId} LocalUserId={LocalUserId} AuthUserId={AuthUserId}",
                currentTenant.TenantId, u.Id, id);
            await auditService.WriteAsync(
                "user.unregistered", u.Id, id, displayName, ct: ct);
            return deletedFromAuth;
        }

        if (!deletedFromAuth && !string.IsNullOrWhiteSpace(id))
            return false;

        await context.Calculations
            .Where(x => x.IsPrivate && x.CreatedBy == u.Id)
            .ExecuteDeleteAsync(ct);

        // Don't leave a department pointing at a deleted head user.
        await context.Department
            .Where(d => d.HeadUserId == u.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.HeadUserId, (int?)null), ct);

        var deletedUserName = DisplayName(u.FirstName, u.LastName, u.Email);
        context.User.Remove(u);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Tenant user deleted. TenantId={TenantId} LocalUserId={LocalUserId} AuthUserId={AuthUserId}",
            currentTenant.TenantId, userid, id);
        await auditService.WriteAsync(
            "user.deleted", userid, id, deletedUserName, ct: ct);
        return true;
    }

    public async Task<int> CountTenantAdminsAsync(CancellationToken ct = default)
    {
        var admins = await userManager.GetUsersInRoleAsync(PMRolesConst.Tenant.Admin);
        return admins.Count(u => u.TenantId == currentTenant.TenantId);
    }

    /// <summary>True when <paramref name="user"/> is a tenant admin and the only one left in the tenant.</summary>
    private async Task<bool> IsLastTenantAdminAsync(ApplicationUser user)
    {
        if (!await userManager.IsInRoleAsync(user, PMRolesConst.Tenant.Admin))
            return false;

        var admins = await userManager.GetUsersInRoleAsync(PMRolesConst.Tenant.Admin);
        return admins.Count(u => u.TenantId == currentTenant.TenantId) <= 1;
    }

    public async Task<bool> ResetPasswordAsync(string authId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authId))
            return false;

        var user = await userManager.FindByIdAsync(authId);
        if (user is null || user.TenantId != currentTenant.TenantId)
            return false;

        var password = IdentityUserSyncHelper.GenerateTemporaryPassword();
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
            return false;

        await userManager.UpdateSecurityStampAsync(user);

        try
        {
            await accountNotificationEmailSender.SendUserCreatedAsync(
                user.Email ?? string.Empty,
                $"{user.Firstname} {user.Lastname}".Trim(),
                password,
                passwordWasGenerated: true,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Password was reset for {Email} but the notification email could not be sent.", user.Email);
        }

        logger.LogInformation(
            "Tenant user password reset. TenantId={TenantId} AuthUserId={AuthUserId}",
            currentTenant.TenantId, user.Id);
        await auditService.WriteAsync(
            "user.password-reset",
            user.UserId,
            user.Id,
            DisplayName(user.Firstname, user.Lastname, user.Email),
            ct: ct);
        return true;
    }

    public async Task<bool> SetLockoutAsync(string authId, bool locked, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authId))
            return false;

        var user = await userManager.FindByIdAsync(authId);
        if (user is null || user.TenantId != currentTenant.TenantId)
            return false;

        // Never lock out the last *active* administrator — it would leave the tenant unable to sign in
        // as an admin (locking by role count alone misses the "both admins locked one-by-one" case).
        if (locked && await userManager.IsInRoleAsync(user, PMRolesConst.Tenant.Admin))
        {
            var (_, activeAdminIds) = await GetTenantAdminSetsAsync();
            if (activeAdminIds.Contains(user.Id) && activeAdminIds.Count <= 1)
                return false;
        }

        if (locked)
        {
            user.LockoutEnabled = true;
            user.LockoutStart = DateTimeOffset.UtcNow;
            user.LockoutEnd = DateTimeOffset.MaxValue;
        }
        else
        {
            user.LockoutStart = null;
            user.LockoutEnd = null;
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return false;

        // Invalidate any active session so a lock takes effect immediately.
        await userManager.UpdateSecurityStampAsync(user);
        logger.LogInformation(
            "Tenant user lock state changed. TenantId={TenantId} AuthUserId={AuthUserId} Locked={Locked}",
            currentTenant.TenantId, user.Id, locked);
        await auditService.WriteAsync(
            locked ? "user.locked" : "user.unlocked",
            user.UserId,
            user.Id,
            DisplayName(user.Firstname, user.Lastname, user.Email),
            locked ? "Användaren spärrades tills vidare" : "Spärren togs bort",
            ct: ct);
        return true;
    }

    public async Task<bool> SetActiveAsync(string authId, bool active, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authId))
            return false;

        var user = await userManager.FindByIdAsync(authId);
        if (user is null || user.TenantId != currentTenant.TenantId)
            return false;

        if (user.IsActive == active)
            return true;

        // Deactivating the last usable administrator would leave the tenant without admin access.
        if (!active && await userManager.IsInRoleAsync(user, PMRolesConst.Tenant.Admin))
        {
            var (_, activeAdminIds) = await GetTenantAdminSetsAsync();
            if (activeAdminIds.Contains(user.Id) && activeAdminIds.Count <= 1)
                return false;
        }

        user.IsActive = active;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return false;

        // Invalidate any active session so deactivation takes effect immediately.
        await userManager.UpdateSecurityStampAsync(user);
        logger.LogInformation(
            "Tenant user active state changed. TenantId={TenantId} AuthUserId={AuthUserId} Active={Active}",
            currentTenant.TenantId, user.Id, active);
        await auditService.WriteAsync(
            active ? "user.activated" : "user.deactivated",
            user.UserId,
            user.Id,
            DisplayName(user.Firstname, user.Lastname, user.Email),
            ct: ct);
        return true;
    }

    public async Task<int> SetDepartmentAsync(IReadOnlyCollection<int> userIds, int? departmentId, CancellationToken ct = default)
    {
        if (userIds is null || userIds.Count == 0)
            return 0;

        var normalizedDepartmentId = departmentId.HasValue && departmentId.Value > 0 ? departmentId : null;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var localUsers = await context.User
            .Where(x => userIds.Contains(x.Id))
            .ToListAsync(ct);

        if (localUsers.Count == 0)
            return 0;

        // Remember where each user came from so the audit log can say "Flyttad från X till Y".
        var previousDepartmentIds = localUsers.ToDictionary(x => x.Id, x => x.DepartmentId);

        foreach (var localUser in localUsers)
        {
            localUser.SetDepartment(normalizedDepartmentId);
            context.User.Update(localUser);
        }

        await context.SaveChangesAsync(ct);

        foreach (var localUser in localUsers)
        {
            var departmentIds = normalizedDepartmentId.HasValue
                ? new[] { normalizedDepartmentId.Value }
                : Array.Empty<int>();
            await SyncUserDepartmentAccessesAsync(context, localUser, departmentIds, ct);
        }

        await context.SaveChangesAsync(ct);

        // A user that leaves a department must not stay registered as that department's head.
        var movedIds = localUsers.Select(x => x.Id).ToList();
        await context.Department
            .Where(d => d.HeadUserId != null
                && movedIds.Contains(d.HeadUserId.Value)
                && (normalizedDepartmentId == null || d.Id != normalizedDepartmentId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.HeadUserId, (int?)null), ct);

        // Keep the linked auth records in sync so role/department checks stay consistent.
        foreach (var localUser in localUsers.Where(x => !string.IsNullOrWhiteSpace(x.ExternalAuthId)))
        {
            var authUser = await userManager.FindByIdAsync(localUser.ExternalAuthId);
            if (authUser is null || authUser.TenantId != currentTenant.TenantId)
                continue;

            authUser.DepartmentId = normalizedDepartmentId;
            await userManager.UpdateAsync(authUser);
        }

        logger.LogInformation(
            "Tenant users moved. TenantId={TenantId} UserCount={UserCount} DepartmentId={DepartmentId}",
            currentTenant.TenantId, localUsers.Count, normalizedDepartmentId);
        var newDepartmentName = await DepartmentNameAsync(context, normalizedDepartmentId, ct);
        foreach (var localUser in localUsers)
        {
            var previousDepartmentId = previousDepartmentIds.GetValueOrDefault(localUser.Id);
            var details = previousDepartmentId == normalizedDepartmentId
                ? $"Avdelning: {newDepartmentName}"
                : $"Flyttad från {await DepartmentNameAsync(context, previousDepartmentId, ct)} till {newDepartmentName}";

            await auditService.WriteAsync(
                "user.department-changed",
                localUser.Id,
                localUser.ExternalAuthId,
                DisplayName(localUser.FirstName, localUser.LastName, localUser.Email),
                details,
                ct: ct);
        }
        return localUsers.Count;
    }

    public async Task<bool> SetRoleAsync(string authId, string role, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authId))
            return false;

        var normalizedRole = IdentityUserSyncHelper.NormalizeRoleForUserScope(role, isAppUser: false);
        if (normalizedRole is null)
            return false;

        var user = await userManager.FindByIdAsync(authId);
        if (user is null || user.TenantId != currentTenant.TenantId)
            return false;

        // Block demoting the last administrator out of the Admin role.
        if (normalizedRole != PMRolesConst.Tenant.Admin && await IsLastTenantAdminAsync(user))
            return false;

        // Read the current role before it changes so the log can say "Roll ändrad från X till Y".
        var previousRole = (await userManager.GetRolesAsync(user)).FirstOrDefault();

        if (!await IdentityUserSyncHelper.EnsureSingleRoleAsync(userManager, user, normalizedRole))
            return false;

        // Admins are tenant-wide and must not belong to a department.
        if (normalizedRole == PMRolesConst.Tenant.Admin && user.DepartmentId.HasValue)
        {
            user.DepartmentId = null;
            await userManager.UpdateAsync(user);

            if (user.UserId.HasValue)
            {
                await using var context = await dbFactory.CreateDbContextAsync(ct);
                var localUser = await context.User.FirstOrDefaultAsync(x => x.Id == user.UserId.Value, ct);
                if (localUser is not null)
                {
                    localUser.SetDepartment(null);
                    context.User.Update(localUser);
                    await SyncUserDepartmentAccessesAsync(context, localUser, [], ct);
                    await context.SaveChangesAsync(ct);
                }

                // A tenant-wide admin can no longer head a department.
                await context.Department
                    .Where(d => d.HeadUserId == user.UserId.Value)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.HeadUserId, (int?)null), ct);
            }
        }

        await userManager.UpdateSecurityStampAsync(user);
        logger.LogInformation(
            "Tenant user role changed. TenantId={TenantId} AuthUserId={AuthUserId} Role={Role}",
            currentTenant.TenantId, user.Id, normalizedRole);
        var previousRoleLabel = TenantRoleLabelSv(previousRole);
        var newRoleLabel = TenantRoleLabelSv(normalizedRole);
        await auditService.WriteAsync(
            "user.role-changed",
            user.UserId,
            user.Id,
            DisplayName(user.Firstname, user.Lastname, user.Email),
            previousRole is not null && previousRoleLabel != newRoleLabel
                ? $"Behörighet ändrad från {previousRoleLabel} till {newRoleLabel}"
                : $"Behörighet: {newRoleLabel}",
            ct: ct);
        return true;
    }

    private static string DisplayName(string? firstName, string? lastName, string? fallback)
    {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? fallback ?? "Unknown user" : name;
    }

    /// <summary>Auth ids of every tenant admin, plus the subset that can currently sign in (not locked out).</summary>
    private async Task<(HashSet<string> AdminIds, HashSet<string> ActiveAdminIds)> GetTenantAdminSetsAsync()
    {
        var admins = (await userManager.GetUsersInRoleAsync(PMRolesConst.Tenant.Admin))
            .Where(u => u.TenantId == currentTenant.TenantId)
            .ToList();

        var now = DateTimeOffset.UtcNow;
        var all = admins.Select(a => a.Id).ToHashSet(StringComparer.Ordinal);
        // "Active" = can actually sign in: neither locked out nor deactivated.
        var active = admins
            .Where(a => a.IsActive && !(a.LockoutEnd.HasValue && a.LockoutEnd.Value > now))
            .Select(a => a.Id)
            .ToHashSet(StringComparer.Ordinal);

        return (all, active);
    }

    public async Task<BulkUserActionResult> SetLockoutBulkAsync(IReadOnlyCollection<string> authIds, bool locked, CancellationToken ct = default)
    {
        if (authIds is null || authIds.Count == 0)
            return default;

        var (adminIds, activeAdminIds) = await GetTenantAdminSetsAsync();
        var remainingActiveAdmins = activeAdminIds.Count;

        int ok = 0, fail = 0, skipped = 0;
        foreach (var id in authIds.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            // Locking an admin is only safe while another admin can still sign in.
            if (locked && adminIds.Contains(id))
            {
                var isActive = activeAdminIds.Contains(id);
                if (isActive && remainingActiveAdmins <= 1)
                {
                    skipped++;
                    continue;
                }

                if (await SetLockoutAsync(id, true, ct))
                {
                    ok++;
                    if (isActive) remainingActiveAdmins--;
                }
                else fail++;

                continue;
            }

            if (await SetLockoutAsync(id, locked, ct)) ok++;
            else fail++;
        }

        return new BulkUserActionResult(ok, fail, skipped);
    }

    public async Task<BulkUserActionResult> SetRoleBulkAsync(IReadOnlyCollection<string> authIds, string role, CancellationToken ct = default)
    {
        if (authIds is null || authIds.Count == 0)
            return default;

        var demoting = !string.Equals(role, PMRolesConst.Tenant.Admin, StringComparison.Ordinal);
        var (adminIds, _) = await GetTenantAdminSetsAsync();
        var remainingAdmins = adminIds.Count;

        int ok = 0, fail = 0, skipped = 0;
        foreach (var id in authIds.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var isAdmin = adminIds.Contains(id);
            if (demoting && isAdmin && remainingAdmins <= 1)
            {
                skipped++;
                continue;
            }

            if (await SetRoleAsync(id, role, ct))
            {
                ok++;
                if (demoting && isAdmin) remainingAdmins--;
            }
            else fail++;
        }

        return new BulkUserActionResult(ok, fail, skipped);
    }

    public async Task<BulkUserActionResult> RemoveBulkAsync(IReadOnlyCollection<(string? AuthId, int UserId)> users, CancellationToken ct = default)
    {
        if (users is null || users.Count == 0)
            return default;

        var (adminIds, _) = await GetTenantAdminSetsAsync();

        int ok = 0, fail = 0, skipped = 0;
        foreach (var (authId, userId) in users)
        {
            var isAdmin = !string.IsNullOrWhiteSpace(authId) && adminIds.Contains(authId!);
            if (isAdmin)
            {
                skipped++;
                continue;
            }

            if (await RemoveAsync(authId ?? string.Empty, false, userId, ct))
            {
                ok++;
            }
            else fail++;
        }

        return new BulkUserActionResult(ok, fail, skipped);
    }

    public async Task<List<TenantUserDto>> GetAllTenantUsersAsync(int? department, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var tenantUsersQuery = context.User.AsNoTracking();
        if (department.HasValue)
            tenantUsersQuery = tenantUsersQuery.Where(x =>
                x.DepartmentId == department.Value
                || x.DepartmentAccesses.Any(a => a.DepartmentId == department.Value));

        var users = await tenantUsersQuery
            .Select(x => new TenantUserDto
            {
                Id = x.Id,
                IdAuth = x.ExternalAuthId,
                DepartmentId = x.DepartmentId,
                Username = x.UserName,
                Email = x.Email,
                Firstname = x.FirstName,
                Lastname = x.LastName
            })
            .ToListAsync(ct);

        var userIds = users.Select(u => u.Id).ToList();
        if (userIds.Count > 0)
        {
            var accessRows = await context.UserDepartmentAccesses
                .AsNoTracking()
                .Where(x => userIds.Contains(x.UserId))
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.DepartmentId)
                .Select(x => new { x.UserId, x.DepartmentId })
                .ToListAsync(ct);

            var accessByUser = accessRows
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.DepartmentId).ToList());

            foreach (var user in users)
            {
                if (accessByUser.TryGetValue(user.Id, out var departmentIds))
                    user.DepartmentIds = departmentIds;
            }
        }

        foreach (var user in users.Where(u => u.DepartmentIds.Count == 0 && u.DepartmentId is > 0))
            user.DepartmentIds.Add(user.DepartmentId!.Value);

        return users;
    }
}
