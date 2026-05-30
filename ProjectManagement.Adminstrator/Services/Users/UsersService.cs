using AuthPermissions.Context;
using AuthPermissions.Entity;
using AuthPermissions.Services;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using Domain.Entities.ResourceType;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Seeding;
using ProjectManagement.Adminstrator.Components.Account;
using Persistence.Interceptors;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.Identity;
using ProjectManagement.Shared.DTO.ResourceType;
using ProjectManagement.Shared.DTO.Tenant;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Models.Account;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ProjectManagement.Adminstrator.Services.Users
{
    public class UsersService(
        UserManager<ApplicationUser> _userManager,
        IServiceScopeFactory _scopeFactory,
        IDbContextFactory<ApplicationDbContext> ContextFactory,
        AuthenticationStateProvider _authStateProvider,
        IAccountNotificationEmailSender _accountNotificationEmailSender,
        ILogger<UsersService> _logger) : IUsersService
    {
        private static readonly EmailAddressAttribute EmailValidator = new();

        private async Task<ClaimsPrincipal> GetCurrentPrincipalAsync()
            => (await _authStateProvider.GetAuthenticationStateAsync()).User;

        private async Task<ApplicationUser?> GetCurrentIdentityUserAsync()
        {
            var principal = await GetCurrentPrincipalAsync();
            return principal.Identity?.IsAuthenticated == true
                ? await _userManager.GetUserAsync(principal)
                : null;
        }

        private static bool HasAnyRole(ClaimsPrincipal? principal, string rolesCsv)
        {
            if (principal?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(rolesCsv))
                return false;

            foreach (var role in rolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (principal.IsInRole(role))
                    return true;
            }

            return false;
        }

        private async Task<bool> HasAnyRoleAsync(string rolesCsv)
            => HasAnyRole(await GetCurrentPrincipalAsync(), rolesCsv);

        private static bool IsValidEmail(string? email)
            => !string.IsNullOrWhiteSpace(email) && EmailValidator.IsValid(email.Trim());

        private async Task<bool> EnsureEmailIsAvailableAsync(string? email, int? tenantId, string? currentUserId = null)
        {
            var normalizedEmail = email?.Trim();
            if (!IsValidEmail(normalizedEmail))
                return false;

            var normalizedLookupEmail = _userManager.NormalizeEmail(normalizedEmail!);
            using var appContext = ContextFactory.CreateDbContext();

            var scopedUsers = appContext.Users.AsNoTracking().Where(x => x.NormalizedEmail == normalizedLookupEmail);
            scopedUsers = tenantId.HasValue
                ? scopedUsers.Where(x => x.TenantId == tenantId)
                : scopedUsers.Where(x => !x.TenantId.HasValue);

            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                scopedUsers = scopedUsers.Where(x => x.Id != currentUserId);
            }

            return !await scopedUsers.AnyAsync();
        }

        private async Task<bool> TenantExistsAsync(int tenantId)
        {
            using var appContext = ContextFactory.CreateDbContext();
            return await appContext.Tenants
                .AsNoTracking()
                .AnyAsync(x => x.Id == tenantId);
        }

        private async Task<bool> IsCurrentUserAsync(string userId)
        {
            var currentUser = await GetCurrentIdentityUserAsync();
            return currentUser != null && string.Equals(currentUser.Id, userId, StringComparison.Ordinal);
        }

        private async Task<bool> IsLastSiteAdminAsync(ApplicationUser user)
        {
            if (user.TenantId.HasValue)
                return false;

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(PMRolesConst.APP.Admin, StringComparer.Ordinal))
                return false;

            var admins = await _userManager.GetUsersInRoleAsync(PMRolesConst.APP.Admin);
            return admins.Count <= 1 && admins.Any(x => string.Equals(x.Id, user.Id, StringComparison.Ordinal));
        }

        public async Task<bool> AddBasicCompanyInfoAsync(int tenantId, int? userId = null)
        {
            if (!await HasAnyRoleAsync(PMRolesConst.APP.AdminManger))
                return false;

            if (tenantId <= 0 || !await TenantExistsAsync(tenantId))
                return false;

            await using var dataAccess = await CreateTenantDbContextAsync(tenantId, userId);
            var changed = 0;

            var existingResourceStatuses = await dataAccess.ResourceStatus.ToListAsync();
            foreach (var defaultResourceStatus in TenantSeedCatalog.ResourceStatuses)
            {
                var status = existingResourceStatuses.FirstOrDefault(x =>
                    string.Equals(x.Name, defaultResourceStatus.Name, StringComparison.OrdinalIgnoreCase));

                if (status is null)
                {
                    dataAccess.ResourceStatus.Add(new StatusResourcesEntity(
                        defaultResourceStatus.Name,
                        defaultResourceStatus.Color,
                        defaultResourceStatus.SortOrder,
                        defaultResourceStatus.IsVisible));
                    continue;
                }

                status.Update(
                    defaultResourceStatus.Name,
                    defaultResourceStatus.Color,
                    defaultResourceStatus.SortOrder,
                    defaultResourceStatus.IsVisible);
            }

            var existingTaskStatuses = await dataAccess.TaskStatus.ToListAsync();
            foreach (var defaultTaskStatus in TenantSeedCatalog.TaskStatuses)
            {
                var status = existingTaskStatuses.FirstOrDefault(x =>
                    string.Equals(x.Name, defaultTaskStatus.Name, StringComparison.OrdinalIgnoreCase));

                if (status is null)
                {
                    dataAccess.TaskStatus.Add(new TaskStatusEntity(
                        defaultTaskStatus.Name,
                        defaultTaskStatus.Color,
                        defaultTaskStatus.SortOrder,
                        defaultTaskStatus.IsVisible));
                    continue;
                }

                status.Update(
                    defaultTaskStatus.Name,
                    defaultTaskStatus.Color,
                    defaultTaskStatus.SortOrder,
                    defaultTaskStatus.IsVisible);
            }

            var existingAccountGroups = await dataAccess.AccountGroup.ToListAsync();
            foreach (var groupName in TenantSeedCatalog.AccountGroups)
            {
                var accountGroup = existingAccountGroups.FirstOrDefault(x =>
                    string.Equals(x.Name, groupName, StringComparison.OrdinalIgnoreCase));

                if (accountGroup is null)
                {
                    dataAccess.AccountGroup.Add(new AccountGroupEntity(groupName));
                    continue;
                }

                accountGroup.Update(groupName);
            }

            if (dataAccess.ChangeTracker.HasChanges())
            {
                changed += await dataAccess.SaveChangesAsync();
            }

            var accountGroupsByName = await dataAccess.AccountGroup
                .ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase);

            var existingAccounts = await dataAccess.Accounts.ToListAsync();
            foreach (var defaultAccount in TenantSeedCatalog.Accounts)
            {
                if (!accountGroupsByName.TryGetValue(defaultAccount.GroupName, out var accountGroup))
                {
                    continue;
                }

                var account = existingAccounts.FirstOrDefault(x =>
                    string.Equals(x.Code, defaultAccount.Code, StringComparison.OrdinalIgnoreCase));

                if (account is null)
                {
                    dataAccess.Accounts.Add(new AccountEntity(
                        defaultAccount.Code,
                        defaultAccount.Name,
                        accountGroup.Id,
                        defaultAccount.IsVisible,
                        new AccountData()));
                    continue;
                }

                account.Update(
                    defaultAccount.Code,
                    defaultAccount.Name,
                    accountGroup.Id,
                    defaultAccount.IsVisible,
                    account.Metadata);
            }

            if (!await dataAccess.Department.AnyAsync())
            {
                dataAccess.Department.AddRange(
                    DepartmentEntity.Create(new DepartmentBase { Name = "Management", Description = "Default management department" }),
                    DepartmentEntity.Create(new DepartmentBase { Name = "Engineering", Description = "Default engineering department" }));
            }

            var existingResourceTypes = await dataAccess.ResourceTypes.ToListAsync();
            foreach (var (type, order) in TenantSeedCatalog.ResourceTypes())
            {
                var resourceTypeDto = new PostResourceTypeDTO
                {
                    Name = type.ToString(),
                    IsVisible = true,
                    Type = type,
                };

                var resourceType = existingResourceTypes.FirstOrDefault(x => x.Kind == type);
                if (resourceType is null)
                {
                    dataAccess.ResourceTypes.Add(ResourceTypeEntity.Create(resourceTypeDto, order));
                    continue;
                }

                resourceType.Update(resourceTypeDto);
                resourceType.UpdateOrder(order);
            }

            if (!await dataAccess.Compensations.AnyAsync())
            {
                foreach (var compensation in TenantSeedCatalog.Compensations)
                {
                    dataAccess.Compensations.Add(new CompensationEntity(
                        compensation.Name,
                        compensation.Color,
                        compensation.SortOrder,
                        compensation.IsVisible));
                }
            }

            var existingContracts = await dataAccess.Contracts.ToListAsync();
            foreach (var defaultContract in TenantSeedCatalog.Contracts)
            {
                var contract = existingContracts.FirstOrDefault(x =>
                    string.Equals(x.Name, defaultContract.Name, StringComparison.OrdinalIgnoreCase));

                if (contract is null)
                {
                    dataAccess.Contracts.Add(new ContractEntity(
                        defaultContract.Name,
                        defaultContract.Color,
                        defaultContract.SortOrder,
                        defaultContract.IsVisible));
                    continue;
                }

                contract.Update(
                    defaultContract.Name,
                    defaultContract.Color,
                    defaultContract.SortOrder,
                    defaultContract.IsVisible);
            }

            var existingProcurementMethods = await dataAccess.ProcurementMethod.ToListAsync();
            foreach (var defaultProcurementMethod in TenantSeedCatalog.ProcurementMethods)
            {
                var procurementMethod = existingProcurementMethods.FirstOrDefault(x =>
                    string.Equals(x.Name, defaultProcurementMethod.Name, StringComparison.OrdinalIgnoreCase));

                if (procurementMethod is null)
                {
                    var newProcurementMethod = new ProcurementMethodEntity();
                    newProcurementMethod.Update(
                        defaultProcurementMethod.Name,
                        defaultProcurementMethod.Color,
                        defaultProcurementMethod.SortOrder,
                        defaultProcurementMethod.IsVisible);
                    dataAccess.ProcurementMethod.Add(newProcurementMethod);
                    continue;
                }

                procurementMethod.Update(
                    defaultProcurementMethod.Name,
                    defaultProcurementMethod.Color,
                    defaultProcurementMethod.SortOrder,
                    defaultProcurementMethod.IsVisible);
            }

            var existingCalcStatuses = await dataAccess.CalculationStatus.ToListAsync();
            foreach (var defaultStatus in TenantSeedCatalog.CalculationStatuses)
            {
                var calcStatus = existingCalcStatuses.FirstOrDefault(x =>
                    string.Equals(x.Name, defaultStatus.Name, StringComparison.OrdinalIgnoreCase));

                if (calcStatus is null)
                {
                    var newStatus = new StatusEntity();
                    newStatus.Update(
                        defaultStatus.Name,
                        defaultStatus.Color,
                        defaultStatus.SortOrder,
                        defaultStatus.IsVisible);
                    newStatus.SetApprovalSettings(
                        defaultStatus.IsApprovalStatus,
                        defaultStatus.LocksCalculation,
                        defaultStatus.AllowsProductionCalculation);
                    dataAccess.CalculationStatus.Add(newStatus);
                    continue;
                }

                calcStatus.Update(
                    defaultStatus.Name,
                    defaultStatus.Color,
                    defaultStatus.SortOrder,
                    defaultStatus.IsVisible);
                calcStatus.SetApprovalSettings(
                    defaultStatus.IsApprovalStatus,
                    defaultStatus.LocksCalculation,
                    defaultStatus.AllowsProductionCalculation);
            }

            if (!await dataAccess.CalcProjectType.AnyAsync())
            {
                foreach (var defaultType in TenantSeedCatalog.ProjectTypes)
                {
                    var type = new TypeEntity();
                    type.Update(
                        defaultType.Name,
                        defaultType.Color,
                        defaultType.SortOrder,
                        defaultType.IsVisible);
                    dataAccess.CalcProjectType.Add(type);
                }
            }

            if (dataAccess.ChangeTracker.HasChanges())
            {
                changed += await dataAccess.SaveChangesAsync();
            }

            return changed > 0;
        }

        public async Task<List<GetTenantsDTO>> GetAsync()
        {
            if (!await HasAnyRoleAsync(PMRolesConst.APP.AdminManger))
                return [];

            using var appContext = ContextFactory.CreateDbContext();
            return await appContext.Tenants
                .AsNoTracking()
                .Select(x => new GetTenantsDTO
                {
                    Name = x.Name,
                    DateExpire = x.DateExpire,
                    DB = x.TenantDB != null ? x.TenantDB.Name : string.Empty,
                    Id = x.Id,
                    DatabaseInfoName = x.TenantDB != null ? x.TenantDB.Name : string.Empty,
                })
                .ToListAsync();
        }

        public async Task<TenantEntity> GetByIdAsync(int id)
        {
            if (!await HasAnyRoleAsync(PMRolesConst.APP.AdminManger))
                return new TenantEntity() { TenantDBId = 0 };

            using var appContext = ContextFactory.CreateDbContext();
            return await appContext.Tenants.FirstOrDefaultAsync(x => x.Id == id) ?? new TenantEntity() { TenantDBId = 0 };
        }

        public async Task<int> CreateAsync(TenantEntity tenant)
        {
            if (!await HasAnyRoleAsync(PMRolesConst.APP.AdminManger))
                return 0;

            if (tenant == null || string.IsNullOrWhiteSpace(tenant.Name))
                return 0;

            using var appContext = ContextFactory.CreateDbContext();

            var normalizedName = tenant.Name.Trim();
            var exists = await appContext.Tenants
                .AsNoTracking()
                .AnyAsync(x => x.Name == normalizedName);
            if (exists)
                return 0;
            var t = new TenantEntity
            {
                Name = normalizedName,
                Street = tenant.Street,
                City = tenant.City,
                Country = tenant.Country,
                Fax = tenant.Fax,
                BuildNumber = tenant.BuildNumber,
                PostCode = tenant.PostCode,
                MaxCalculations = tenant.MaxCalculations,
                MaxUsers = tenant.MaxUsers,
                Website = tenant.Website,
                Phone = tenant.Phone,
                Mobile = tenant.Mobile,
                Email = tenant.Email,
                DateExpire = tenant.DateExpire,
                Note = tenant.Note,
                TenantDBId = tenant.TenantDBId
            };

            appContext.Tenants.Add(t);
            await appContext.SaveChangesAsync();
            return t.Id;
        }

        public async Task<bool> UpdateAsync(TenantEntity tenant)
        {
            if (!await HasAnyRoleAsync(PMRolesConst.APP.AdminManger))
                return false;

            if (tenant == null || tenant.Id <= 0 || string.IsNullOrWhiteSpace(tenant.Name))
                return false;

            using var appContext = ContextFactory.CreateDbContext();
            var t = await appContext.Tenants.FirstOrDefaultAsync(x => x.Id == tenant.Id);
            if (t == null)
                return false;

            var normalizedName = tenant.Name.Trim();
            var duplicateNameExists = await appContext.Tenants
                .AsNoTracking()
                .AnyAsync(x => x.Id != tenant.Id && x.Name == normalizedName);
            if (duplicateNameExists)
                return false;

            t.Name = normalizedName;
            t.Street = tenant.Street;
            t.City = tenant.City;
            t.Country = tenant.Country;
            t.Fax = tenant.Fax;
            t.BuildNumber = tenant.BuildNumber;
            t.PostCode = tenant.PostCode;
            t.MaxCalculations = tenant.MaxCalculations;
            t.MaxUsers = tenant.MaxUsers;
            t.Website = tenant.Website;
            t.Phone = tenant.Phone;
            t.Mobile = tenant.Mobile;
            t.Email = tenant.Email;
            t.DateExpire = tenant.DateExpire;
            t.Note = tenant.Note;
            t.TenantDBId = tenant.TenantDBId;

            appContext.Tenants.Update(t);
            await appContext.SaveChangesAsync();
            return true;
        }

        public async Task<IList<string>> GetRolesAsync(string username)
        {
            if (!await HasAnyRoleAsync(PMRolesConst.APP.AdminManger))
                return [];

            using var scope = _scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            ApplicationUser? user = null;

            if (!string.IsNullOrWhiteSpace(username))
            {
                user = await userManager.FindByNameAsync(username)
                    ?? await userManager.FindByEmailAsync(username)
                    ?? await userManager.FindByIdAsync(username);
            }

            if (user == null)
                return new List<string>();

            return await userManager.GetRolesAsync(user);
        }

        private async Task<bool> CanManageUserScopeAsync(int? tenantId, bool writeOperation)
        {
            if (tenantId.HasValue)
            {
                return await HasAnyRoleAsync(writeOperation
                    ? PMRolesConst.APP.AdminManger
                    : PMRolesConst.APP.User);
            }

            return await HasAnyRoleAsync(writeOperation
                ? PMRolesConst.APP.Admin
                : PMRolesConst.APP.AdminManger);
        }

        private async Task<ShardingSingleDbContext> CreateTenantDbContextAsync(int tenantId, int? currentUserId = null)
        {
            using var appContext = ContextFactory.CreateDbContext();
            var connectionString = await appContext.Tenants
                .Where(x => x.Id == tenantId)
                .Select(x => x.TenantDB != null ? x.TenantDB.ConnectionString : null)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"No database found for tenant {tenantId}");

            var optionsBuilder = new DbContextOptionsBuilder<ShardingSingleDbContext>();
            optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });
            optionsBuilder.AddInterceptors(new TenantAuditSaveChangesInterceptor());

            return new ShardingSingleDbContext(optionsBuilder.Options)
            {
                TenantId = tenantId,
                CurrentUserId = currentUserId
            };
        }

        public Task<ShardingSingleDbContext> CreateDbContext(int tenantId)
            => CreateTenantDbContextAsync(tenantId, null);

        public async Task<bool> RemoveTenant(int TenantId)
        {
            if (!await HasAnyRoleAsync(PMRolesConst.APP.Admin))
                return false;

            if (TenantId <= 0 || !await TenantExistsAsync(TenantId))
                return false;

            await using var dataAccess = await CreateTenantDbContextAsync(TenantId);

            dataAccess.User.RemoveRange(dataAccess.User.Where(x => x.TenantId == TenantId));
            dataAccess.Department.RemoveRange(dataAccess.Department.Where(x => x.TenantId == TenantId));
            dataAccess.AccountGroup.RemoveRange(dataAccess.AccountGroup.Where(x => x.TenantId == TenantId));
            dataAccess.Applications.RemoveRange(dataAccess.Applications.Where(x => x.TenantId == TenantId));
            dataAccess.OrganisationCategory.RemoveRange(dataAccess.OrganisationCategory.Where(x => x.TenantId == TenantId));
            await dataAccess.SaveChangesAsync();

            dataAccess.Folders.RemoveRange(dataAccess.Folders.Where(x => x.TenantId == TenantId));
            dataAccess.OrganisationType.RemoveRange(dataAccess.OrganisationType.Where(x => x.TenantId == TenantId));
            await dataAccess.SaveChangesAsync();

            dataAccess.Opportunity.RemoveRange(dataAccess.Opportunity.Where(x => x.TenantId == TenantId));
            dataAccess.ProcurementMethod.RemoveRange(dataAccess.ProcurementMethod.Where(x => x.TenantId == TenantId));
            dataAccess.Compensations.RemoveRange(dataAccess.Compensations.Where(x => x.TenantId == TenantId));
            dataAccess.Contracts.RemoveRange(dataAccess.Contracts.Where(x => x.TenantId == TenantId));
            dataAccess.CalculationStatus.RemoveRange(dataAccess.CalculationStatus.Where(x => x.TenantId == TenantId));
            dataAccess.ResourceStatus.RemoveRange(dataAccess.ResourceStatus.Where(x => x.TenantId == TenantId));
            dataAccess.Templates.RemoveRange(dataAccess.Templates.Where(x => x.TenantId == TenantId));
            await dataAccess.SaveChangesAsync();

            using var appContext = ContextFactory.CreateDbContext();
            var users = await appContext.Users.Where(x => x.TenantId == TenantId).ToListAsync();
            if (users.Count > 0)
            {
                appContext.Users.RemoveRange(users);
                await appContext.SaveChangesAsync();
            }

            var tenant = await appContext.Tenants.FirstOrDefaultAsync(x => x.Id == TenantId);
            if (tenant is null)
                return false;

            appContext.Tenants.Remove(tenant);
            await appContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateUserAsync(UserPostDTO user, int? tentnid)
        {
            if (!await CanManageUserScopeAsync(tentnid, writeOperation: true))
                return false;

            if (user == null || string.IsNullOrWhiteSpace(user.Id) || !await EnsureEmailIsAvailableAsync(user.Email, tentnid, user.Id))
                return false;

            var oldUser = await _userManager.FindByIdAsync(user.Id);
            if (oldUser == null || oldUser.TenantId != tentnid)
                return false;

            var isAppUser = !tentnid.HasValue;
            var normalizedRole = IdentityUserSyncHelper.NormalizeRoleForUserScope(user.Role, isAppUser);
            if (normalizedRole is null)
                return false;

            var isCurrentUser = await IsCurrentUserAsync(oldUser.Id);
            var isLastSiteAdmin = await IsLastSiteAdminAsync(oldUser);

            if (isAppUser && oldUser.TenantId.HasValue)
                return false;

            if (isAppUser && isCurrentUser)
            {
                if (user.LockoutEnabled)
                    return false;

                if (!string.Equals(normalizedRole, PMRolesConst.APP.Admin, StringComparison.Ordinal))
                    return false;
            }

            if (isAppUser && isLastSiteAdmin)
            {
                if (user.LockoutEnabled)
                    return false;

                if (!string.Equals(normalizedRole, PMRolesConst.APP.Admin, StringComparison.Ordinal))
                    return false;
            }

            IdentityUserSyncHelper.ApplyToIdentityUser(
                oldUser,
                user.Email,
                user.Email,
                tentnid,
                user.DepartmentId,
                oldUser.UserId,
                user.Firstname,
                user.Lastname,
                user.PhoneNumber,
                user.PhoneNumberConfirmed,
                user.LockoutEnabled,
                user.LockoutStart,
                user.LockoutEnd,
                isAppUser);

            if (tentnid.HasValue)
            {
                await using var dataAccess = await CreateTenantDbContextAsync(tentnid.Value);

                UserEntity? userEntity = null;
                if (oldUser.UserId.HasValue)
                {
                    userEntity = await dataAccess.User.FindAsync(oldUser.UserId.Value);
                }

                userEntity ??= await dataAccess.User.FirstOrDefaultAsync(x => x.ExternalAuthId == user.Id);
                if (userEntity == null)
                    return false;

                IdentityUserSyncHelper.ApplyToLocalUser(
                    userEntity,
                    user.Email,
                    user.Email,
                    user.DepartmentId,
                    user.Firstname,
                    user.Lastname,
                    user.Id);

                await dataAccess.SaveChangesAsync();
                oldUser.UserId = userEntity.Id;
            }

            var updateResult = await _userManager.UpdateAsync(oldUser);
            if (!updateResult.Succeeded)
                return false;

            if (!await IdentityUserSyncHelper.EnsureSingleRoleAsync(_userManager, oldUser, normalizedRole))
                return false;

            await _userManager.UpdateSecurityStampAsync(oldUser);
            return true;
        }

        public async Task<bool> RemoveUserAsync(string id, int? tenantId)
        {
            if (!await CanManageUserScopeAsync(tenantId, writeOperation: true))
                return false;

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return false;

            if (tenantId.HasValue)
            {
                if (user.TenantId != tenantId)
                    return false;
            }
            else if (user.TenantId.HasValue)
            {
                return false;
            }

            if (!tenantId.HasValue)
            {
                if (await IsCurrentUserAsync(user.Id))
                    return false;

                if (await IsLastSiteAdminAsync(user))
                    return false;
            }

            if (tenantId.HasValue && user.TenantId.HasValue)
            {
                await using var dataAccess = await CreateTenantDbContextAsync(user.TenantId.Value);
                UserEntity? localUser = null;

                if (user.UserId.HasValue)
                {
                    localUser = await dataAccess.User.FindAsync(user.UserId.Value);
                }

                localUser ??= await dataAccess.User.FirstOrDefaultAsync(x => x.ExternalAuthId == id);
                if (localUser == null)
                    return false;

                await dataAccess.Calculations
                    .Where(x => x.IsPrivate && x.CreatedBy == localUser.Id)
                    .ExecuteDeleteAsync();

                dataAccess.User.Remove(localUser);
                await dataAccess.SaveChangesAsync();
            }

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> BlockTenantAsync(int TenantId, bool block)
        {
            if (!await HasAnyRoleAsync(PMRolesConst.APP.AdminManger))
                return false;

            if (TenantId <= 0 || !await TenantExistsAsync(TenantId))
                return false;

            using var appContext = ContextFactory.CreateDbContext();
            var users = await appContext.Users.Where(x => x.TenantId == TenantId).ToListAsync();
            if (users.Count == 0)
                return false;

            foreach (var item in users)
            {
                item.LockoutEnabled = block;
                item.LockoutStart = block ? DateTimeOffset.UtcNow : null;
                item.LockoutEnd = block ? DateTimeOffset.UtcNow.AddYears(100) : null;
            }

            await appContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BlockTenantAsync(string userid, bool block)
        {
            if (!await HasAnyRoleAsync(PMRolesConst.APP.Admin))
                return false;

            using var appContext = ContextFactory.CreateDbContext();
            var user = await appContext.Users.FirstOrDefaultAsync(x => x.Id == userid);
            if (user == null || user.TenantId.HasValue)
                return false;

            if (block && await IsCurrentUserAsync(user.Id))
                return false;

            if (block && await IsLastSiteAdminAsync(user))
                return false;

            user.LockoutEnabled = block;
            user.LockoutStart = block ? DateTimeOffset.UtcNow : null;
            user.LockoutEnd = block ? DateTimeOffset.UtcNow.AddYears(100) : null;
            await appContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<ApplicationUser>> GetUsersAsync(int? TenantId, int? department = null)
        {
            if (!await CanManageUserScopeAsync(TenantId, writeOperation: false))
                return [];

            using var appContext = ContextFactory.CreateDbContext();
            var users = appContext.Users.AsQueryable();

            if (TenantId.HasValue && TenantId > 0)
            {
                users = users.Where(x => x.TenantId == TenantId);
                if (department.HasValue && department > 0)
                {
                    users = users.Where(x => x.DepartmentId == department);
                }
            }
            else
            {
                users = users.Where(x => !x.TenantId.HasValue);
            }

            return await users.OrderBy(x => x.Email).ToListAsync();
        }

        public async Task<bool> RegisterAsync(UserPostDTO request, int? tenantId)
        {
            if (!await CanManageUserScopeAsync(tenantId, writeOperation: true))
                return false;

            if (request == null)
                return false;

            if (!await EnsureEmailIsAvailableAsync(request.Email, tenantId))
                return false;

            if (tenantId.HasValue && !await TenantExistsAsync(tenantId.Value))
                return false;

            var isAppUser = !tenantId.HasValue;
            var normalizedRole = IdentityUserSyncHelper.NormalizeRoleForUserScope(request.Role, isAppUser);
            if (normalizedRole is null)
                return false;

            ShardingSingleDbContext? tenantDb = null;
            UserEntity? localUser = null;
            ApplicationUser? identityUser = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(request.Password) &&
                    !string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
                {
                    return false;
                }

                if (tenantId.HasValue)
                {
                    tenantDb = await CreateTenantDbContextAsync(tenantId.Value);
                    var normalizedEmail = request.Email.Trim();
                    localUser = UserEntity.Create(
                        tenantId.Value,
                        normalizedEmail,
                        normalizedEmail,
                        request.DepartmentId,
                        request.Firstname,
                        request.Lastname,
                        null);

                    tenantDb.User.Add(localUser);
                    await tenantDb.SaveChangesAsync();
                }

                identityUser = new ApplicationUser
                {
                    EmailConfirmed = true
                };

                IdentityUserSyncHelper.ApplyToIdentityUser(
                    identityUser,
                    request.Email,
                    request.Email,
                    tenantId,
                    request.DepartmentId,
                    localUser?.Id,
                    request.Firstname,
                    request.Lastname,
                    request.PhoneNumber,
                    request.PhoneNumberConfirmed,
                    request.LockoutEnabled,
                    request.LockoutStart,
                    request.LockoutEnd,
                    isAppUser);

                var password = string.IsNullOrWhiteSpace(request.Password)
                    ? IdentityUserSyncHelper.GenerateTemporaryPassword()
                    : request.Password.Trim();

                var createResult = await _userManager.CreateAsync(identityUser, password);
                if (!createResult.Succeeded)
                {
                    if (tenantDb != null && localUser != null)
                    {
                        tenantDb.User.Remove(localUser);
                        await tenantDb.SaveChangesAsync();
                    }

                    return false;
                }

                if (!await IdentityUserSyncHelper.EnsureSingleRoleAsync(_userManager, identityUser, normalizedRole))
                {
                    await _userManager.DeleteAsync(identityUser);

                    if (tenantDb != null && localUser != null)
                    {
                        tenantDb.User.Remove(localUser);
                        await tenantDb.SaveChangesAsync();
                    }

                    return false;
                }

                if (tenantDb != null && localUser != null)
                {
                    localUser.SetExternalAuthId(identityUser.Id);
                    tenantDb.User.Update(localUser);
                    await tenantDb.SaveChangesAsync();

                    identityUser.UserId = localUser.Id;
                    var updateResult = await _userManager.UpdateAsync(identityUser);
                    if (!updateResult.Succeeded)
                    {
                        await _userManager.DeleteAsync(identityUser);
                        tenantDb.User.Remove(localUser);
                        await tenantDb.SaveChangesAsync();
                        return false;
                    }
                }

                await _userManager.UpdateSecurityStampAsync(identityUser);

                try
                {
                    await _accountNotificationEmailSender.SendUserCreatedAsync(
                        identityUser.Email ?? request.Email,
                        $"{request.Firstname} {request.Lastname}".Trim(),
                        string.IsNullOrWhiteSpace(request.Password) ? password : null,
                        string.IsNullOrWhiteSpace(request.Password));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "User {Email} was created but the notification email could not be sent.", identityUser.Email ?? request.Email);
                }

                return true;
            }
            catch
            {
                if (identityUser != null && !string.IsNullOrWhiteSpace(identityUser.Id))
                {
                    await _userManager.DeleteAsync(identityUser);
                }

                if (tenantDb != null && localUser != null)
                {
                    tenantDb.User.Remove(localUser);
                    await tenantDb.SaveChangesAsync();
                }

                return false;
            }
        }
    }
}
