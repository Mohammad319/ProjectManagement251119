using AuthPermissions.Context;
using AuthPermissions.Entity;
using AuthPermissions.Services;
using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
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
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Identity;
using ProjectManagement.Shared.DTO.Project;
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
        IDbContextFactory<AuthPermissionDbContext> ContextFactory,
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

        /// <summary>Identifies the acting administrator for audit log entries.</summary>
        private async Task<string> CurrentActorAsync()
        {
            var user = await GetCurrentIdentityUserAsync();
            return user?.Email ?? user?.UserName ?? "unknown";
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

        private async Task<bool> IsLastTenantAdminAsync(ApplicationUser user, int tenantId)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(PMRolesConst.Tenant.Admin, StringComparer.Ordinal))
                return false;

            var rolesMap = await GetUserRolesMapAsync(tenantId);
            var adminCount = rolesMap.Count(kv => string.Equals(kv.Value, PMRolesConst.Tenant.Admin, StringComparison.Ordinal));
            return adminCount <= 1;
        }

        private static void CountPendingChanges(CompanySeedResultDTO result, ShardingSingleDbContext context)
        {
            var entries = context.ChangeTracker.Entries().ToList();
            result.Created += entries.Count(e => e.State == EntityState.Added);
            result.Updated += entries.Count(e => e.State == EntityState.Modified);
        }

        public async Task<CompanySeedResultDTO> AddBasicCompanyInfoAsync(int tenantId, int? userId = null)
        {
            var result = new CompanySeedResultDTO();

            if (!await HasAnyRoleAsync(PMRolesConst.APP.AdminManger))
                return result;

            if (tenantId <= 0 || !await TenantExistsAsync(tenantId))
                return result;

            await using var dataAccess = await CreateTenantDbContextAsync(tenantId, userId);

            // EnableRetryOnFailure on the tenant connection requires an execution strategy
            // to wrap a manual transaction; ChangeTracker.Clear keeps retries idempotent.
            var strategy = dataAccess.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                dataAccess.ChangeTracker.Clear();
                result.Created = 0;
                result.Updated = 0;

                await using var transaction = await dataAccess.Database.BeginTransactionAsync();

                // Phase 1: account groups must be persisted before accounts can reference their ids.
                await SeedResourceStatusesAsync(dataAccess);
                await SeedTaskStatusesAsync(dataAccess);
                await SeedAccountGroupsAsync(dataAccess);
                await SaveCountingAsync(dataAccess, result);

                // Phase 2: everything else (accounts now resolve their saved group ids).
                await SeedAccountsAsync(dataAccess);
                await SeedDepartmentsAsync(dataAccess);
                await SeedResourceTypesAsync(dataAccess);
                await SeedProjectLookupsAsync(dataAccess);
                await SeedCalculationStatusesAsync(dataAccess);
                await SeedProjectStatusesAsync(dataAccess);
                await SaveCountingAsync(dataAccess, result);

                await transaction.CommitAsync();
            });

            return result;
        }

        // Fixed name used as the idempotency marker so re-clicking the button does not duplicate the demo content.
        private const string SampleFolderName = "Demoprojekt";

        public async Task<CompanySeedResultDTO> AddSampleProjectDataAsync(int tenantId, int? userId = null)
        {
            var result = new CompanySeedResultDTO();

            if (!await HasAnyRoleAsync(PMRolesConst.APP.AdminManger))
                return result;

            if (tenantId <= 0 || !await TenantExistsAsync(tenantId))
                return result;

            await using var dataAccess = await CreateTenantDbContextAsync(tenantId, userId);

            var strategy = dataAccess.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                dataAccess.ChangeTracker.Clear();
                result.Created = 0;
                result.Updated = 0;

                await using var transaction = await dataAccess.Database.BeginTransactionAsync();

                // Sample records are owned by a real user; make sure at least one department exists too.
                var departments = await dataAccess.Department.OrderBy(x => x.Id).ToListAsync();
                if (departments.Count == 0)
                {
                    await SeedDepartmentsAsync(dataAccess);
                    await SaveCountingAsync(dataAccess, result);
                    departments = await dataAccess.Department.OrderBy(x => x.Id).ToListAsync();
                }

                var ownerId = userId is > 0
                    ? userId
                    : await dataAccess.User.OrderBy(x => x.Id).Select(x => (int?)x.Id).FirstOrDefaultAsync();

                // Cannot create owned sample data without a department and a user — leave the tenant untouched.
                if (departments.Count == 0 || ownerId is null or <= 0)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                dataAccess.CurrentUserId = ownerId;

                // Idempotent: only seed departments that don't already have the demo folder.
                var seededDepartmentIds = (await dataAccess.Folders
                    .Where(x => x.Name == SampleFolderName)
                    .Select(x => x.DepartmentId)
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet();

                // Optional lookups — referenced only when the tenant already has them seeded.
                var projectStatusId = await dataAccess.ProjectStatus.OrderBy(x => x.SortOrder).Select(x => (int?)x.Id).FirstOrDefaultAsync();
                var projectTypeId = await dataAccess.CalcProjectType.OrderBy(x => x.SortOrder).Select(x => (int?)x.Id).FirstOrDefaultAsync();
                var calcStatusId = await dataAccess.CalculationStatus.OrderBy(x => x.SortOrder).Select(x => (int?)x.Id).FirstOrDefaultAsync();
                var taskStatusId = await dataAccess.TaskStatus.OrderBy(x => x.SortOrder).Select(x => (int?)x.Id).FirstOrDefaultAsync();
                var resourceStatusId = await dataAccess.ResourceStatus.OrderBy(x => x.SortOrder).Select(x => (int?)x.Id).FirstOrDefaultAsync();
                var accountIds = await dataAccess.Accounts.OrderBy(x => x.Id).Select(x => x.Id).ToListAsync();
                var resourceTypeByKind = await dataAccess.ResourceTypes.ToDictionaryAsync(x => x.Kind, x => x.Id);

                // Project codes are unique per tenant (UX_Projects_Tenant_Code), not per folder.
                // Continue numbering after any existing "P-<n>" codes so re-runs, multi-department
                // tenants, and pre-existing projects don't collide on the unique index.
                var nextProjectNumber = 1;
                foreach (var existingCode in await dataAccess.Projects.Select(p => p.Code).ToListAsync())
                {
                    if (!string.IsNullOrEmpty(existingCode)
                        && existingCode.StartsWith("P-", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(existingCode.AsSpan(2), out var n)
                        && n >= nextProjectNumber)
                    {
                        nextProjectNumber = n + 1;
                    }
                }

                // A demo folder is created in every department so any logged-in user sees content in their own department.
                foreach (var department in departments)
                {
                    if (seededDepartmentIds.Contains(department.Id))
                        continue;

                    // Phase 1: folder for this department.
                    var folder = new FolderEntity(SampleFolderName, "#2563EB", department.Id, ownerId.Value, 100);
                    dataAccess.Folders.Add(folder);
                    await SaveCountingAsync(dataAccess, result);

                    // Phase 2: 18 projects under the folder.
                    var projects = new List<ProjectEntity>();
                    for (var i = 0; i < ProjectsPerFolder; i++)
                    {
                        var dto = new PostProjectDTO
                        {
                            Name = $"{SampleProjectNames[i % SampleProjectNames.Length]} {i + 1:00}",
                            Code = $"P-{nextProjectNumber:00}",
                            FolderId = folder.Id,
                            StatusId = projectStatusId,
                            TypeId = projectTypeId,
                        };
                        projects.Add(ProjectEntity.Create(dto, folder.Id, ownerId.Value, (i + 1) * 100));
                        nextProjectNumber++;
                    }
                    dataAccess.Projects.AddRange(projects);
                    await SaveCountingAsync(dataAccess, result);

                    // Phase 3: the first project gets 18 calculations, the rest get one each.
                    var detailedCalcs = new List<CalculationEntity>();
                    for (var pi = 0; pi < projects.Count; pi++)
                    {
                        var isLeadProject = pi == 0;
                        var calcCount = isLeadProject ? CalcsInLeadProject : CalcsPerOtherProject;

                        for (var j = 0; j < calcCount; j++)
                        {
                            var calc = new CalculationEntity();
                            calc.AssignDepartment(department.Id);
                            calc.AssignToProject(projects[pi].Id);
                            calc.Update(new CalculationPostDTO
                            {
                                Name = $"Kalkyl {j + 1:00}",
                                Code = $"K-{j + 1:00}",
                                StatusId = calcStatusId,
                                TypeId = projectTypeId,
                                Order = (j + 1) * 100,
                            });

                            dataAccess.Calculations.Add(calc);

                            // Give the first few calculations of the lead project real tasks/resources to explore.
                            if (isLeadProject && j < DetailedCalcsPerFolder)
                                detailedCalcs.Add(calc);
                        }
                    }
                    await SaveCountingAsync(dataAccess, result);

                    // Phase 4: tasks for the "detailed" calculations.
                    var createdTasks = new List<(TaskEntity Task, bool WithResources)>();
                    foreach (var calc in detailedCalcs)
                    {
                        var taskSort = 100;
                        foreach (var t in SampleTasks)
                        {
                            var task = TaskEntity.Create(calc.Id, new TaskPostDTO
                            {
                                Name = t.Name,
                                Unit = t.Unit,
                                Quantity = t.Quantity,
                                StatusId = taskStatusId,
                            }, taskSort);
                            taskSort += 100;

                            dataAccess.Tasks.Add(task);
                            createdTasks.Add((task, t.WithResources));
                        }
                    }
                    await SaveCountingAsync(dataAccess, result);

                    // Phase 5: resources for tasks that should carry them.
                    foreach (var (task, withResources) in createdTasks)
                    {
                        if (!withResources)
                            continue;

                        var resourceSort = 100;
                        for (var ri = 0; ri < SampleResources.Length; ri++)
                        {
                            var r = SampleResources[ri];
                            resourceTypeByKind.TryGetValue(r.ResType, out var resourceTypeId);

                            var resource = ResourceEntity.Create(new ResourcePostDTO
                            {
                                Name = r.Name,
                                ResType = r.ResType,
                                Unit = r.Unit,
                                Quantity = r.Quantity,
                                SortOrder = resourceSort,
                                StatusId = resourceStatusId,
                                ResourceTypeId = resourceTypeId > 0 ? resourceTypeId : null,
                                AccountId = accountIds.Count > 0 ? accountIds[ri % accountIds.Count] : null,
                                Data = new ResourceMetadata { Cost = r.Cost },
                            }, resourceSort, task.Id);
                            resourceSort += 100;

                            dataAccess.Resources.Add(resource);
                        }
                    }
                    await SaveCountingAsync(dataAccess, result);
                }

                await transaction.CommitAsync();
            });

            return result;
        }

        // Demo data sizing.
        private const int ProjectsPerFolder = 18;
        private const int CalcsInLeadProject = 18;
        private const int CalcsPerOtherProject = 1;
        private const int DetailedCalcsPerFolder = 3;

        private sealed record SampleTask(string Name, string Unit, decimal Quantity, bool WithResources);
        private sealed record SampleResource(string Name, ResourceTypesEnum ResType, string Unit, decimal Quantity, decimal Cost);

        // Project name pool (cycled + numbered to fill the 18 projects per folder).
        private static readonly string[] SampleProjectNames =
        [
            "Nybyggnad flerbostadshus",
            "Renovering skola",
            "Ombyggnad kontor",
            "Vägprojekt",
            "Brounderhåll",
            "VA-ledningar",
            "Markarbete park",
            "Tillbyggnad lager",
            "Energiprojekt",
        ];

        private static readonly SampleTask[] SampleTasks =
        [
            new("Markarbeten", "st", 10m, WithResources: true),
            new("Betongarbeten", "m3", 50m, WithResources: true),
            new("Installationer", "st", 1m, WithResources: false),
        ];

        private static readonly SampleResource[] SampleResources =
        [
            new("Betong C25/30", ResourceTypesEnum.Materials, "m3", 50m, 1200m),
            new("Grävmaskin", ResourceTypesEnum.MachinesAndEquipments, "tim", 16m, 850m),
            new("Anläggningsarbetare", ResourceTypesEnum.Worker, "tim", 80m, 420m),
        ];

        private static async Task SaveCountingAsync(ShardingSingleDbContext ctx, CompanySeedResultDTO result)
        {
            if (!ctx.ChangeTracker.HasChanges())
                return;

            CountPendingChanges(result, ctx);
            await ctx.SaveChangesAsync();
        }

        private static async Task SeedResourceStatusesAsync(ShardingSingleDbContext ctx)
        {
            var existing = await ctx.ResourceStatus.ToListAsync();
            foreach (var d in TenantSeedCatalog.ResourceStatuses)
            {
                var match = existing.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(d.Code) && string.Equals(x.Code, d.Code, StringComparison.OrdinalIgnoreCase))
                    ?? existing.FirstOrDefault(x => string.Equals(x.Name, d.Name, StringComparison.OrdinalIgnoreCase));

                if (match is null)
                {
                    var created = new StatusResourcesEntity(d.Name, d.Color, d.SortOrder, d.IsVisible);
                    created.SetControlStatusSettings(d.Code, d.IsDefault, d.IsSystemDefault);
                    ctx.ResourceStatus.Add(created);
                    continue;
                }

                match.Update(d.Name, d.Color, d.SortOrder, d.IsVisible);
                match.SetControlStatusSettings(
                    string.IsNullOrWhiteSpace(match.Code) ? d.Code : match.Code,
                    d.IsDefault,
                    d.IsSystemDefault || match.IsSystemDefault);
            }
        }

        private static async Task SeedTaskStatusesAsync(ShardingSingleDbContext ctx)
        {
            var existing = await ctx.TaskStatus.ToListAsync();
            foreach (var d in TenantSeedCatalog.TaskStatuses)
            {
                var match = existing.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(d.Code) && string.Equals(x.Code, d.Code, StringComparison.OrdinalIgnoreCase))
                    ?? existing.FirstOrDefault(x => string.Equals(x.Name, d.Name, StringComparison.OrdinalIgnoreCase));

                if (match is null)
                {
                    var created = new TaskStatusEntity(d.Name, d.Color, d.SortOrder, d.IsVisible);
                    created.SetControlStatusSettings(d.Code, d.IsDefault, d.IsSystemDefault);
                    ctx.TaskStatus.Add(created);
                    continue;
                }

                match.Update(d.Name, d.Color, d.SortOrder, d.IsVisible);
                match.SetControlStatusSettings(
                    string.IsNullOrWhiteSpace(match.Code) ? d.Code : match.Code,
                    d.IsDefault,
                    d.IsSystemDefault || match.IsSystemDefault);
            }
        }

        private static async Task SeedAccountGroupsAsync(ShardingSingleDbContext ctx)
        {
            var existing = await ctx.AccountGroup.ToListAsync();
            foreach (var groupName in TenantSeedCatalog.AccountGroups)
            {
                var match = existing.FirstOrDefault(x => string.Equals(x.Name, groupName, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    ctx.AccountGroup.Add(new AccountGroupEntity(groupName));
                    continue;
                }

                match.Update(groupName);
            }
        }

        private static async Task SeedAccountsAsync(ShardingSingleDbContext ctx)
        {
            var groupsByName = await ctx.AccountGroup.ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase);
            var existing = await ctx.Accounts.ToListAsync();
            foreach (var d in TenantSeedCatalog.Accounts)
            {
                if (!groupsByName.TryGetValue(d.GroupName, out var group))
                    continue;

                var match = existing.FirstOrDefault(x => string.Equals(x.Code, d.Code, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    ctx.Accounts.Add(new AccountEntity(d.Code, d.Name, group.Id, d.IsVisible, new AccountData()));
                    continue;
                }

                match.Update(d.Code, d.Name, group.Id, d.IsVisible, match.Metadata);
            }
        }

        private static async Task SeedDepartmentsAsync(ShardingSingleDbContext ctx)
        {
            if (await ctx.Department.AnyAsync())
                return;

            ctx.Department.AddRange(
                DepartmentEntity.Create(new DepartmentBase { Name = "Management", Description = "Default management department" }),
                DepartmentEntity.Create(new DepartmentBase { Name = "Engineering", Description = "Default engineering department" }));
        }

        private static async Task SeedResourceTypesAsync(ShardingSingleDbContext ctx)
        {
            var existing = await ctx.ResourceTypes.ToListAsync();
            foreach (var (type, name, order) in TenantSeedCatalog.ResourceTypes())
            {
                var dto = new PostResourceTypeDTO { Name = name, IsVisible = true, Type = type, Order = order };
                var match = existing.FirstOrDefault(x => x.Kind == type);
                if (match is null)
                {
                    ctx.ResourceTypes.Add(ResourceTypeEntity.Create(dto, order));
                    continue;
                }

                match.Update(dto);
                match.UpdateOrder(order);
            }
        }

        private static async Task SeedCalculationStatusesAsync(ShardingSingleDbContext ctx)
        {
            var existing = await ctx.CalculationStatus.ToListAsync();
            foreach (var d in TenantSeedCatalog.CalculationStatuses)
            {
                var match = existing.FirstOrDefault(x => string.Equals(x.Name, d.Name, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    var created = new StatusEntity();
                    created.Update(d.Name, d.Color, d.SortOrder, d.IsVisible);
                    created.SetApprovalSettings(d.IsApprovalStatus, d.LocksCalculation, d.AllowsProductionCalculation);
                    created.SetHitRateSettings(d.CountsAsSubmittedBid, d.CountsAsWonBid, d.CountsAsLostBid);
                    created.SetIsDefault(d.IsDefault);
                    ctx.CalculationStatus.Add(created);
                    continue;
                }

                match.Update(d.Name, d.Color, d.SortOrder, d.IsVisible);
                match.SetApprovalSettings(d.IsApprovalStatus, d.LocksCalculation, d.AllowsProductionCalculation);
                match.SetHitRateSettings(d.CountsAsSubmittedBid, d.CountsAsWonBid, d.CountsAsLostBid);
                if (d.IsDefault && !existing.Any(x => x.IsDefault))
                    match.SetIsDefault(true);
            }
        }

        private static async Task SeedProjectStatusesAsync(ShardingSingleDbContext ctx)
        {
            var existing = await ctx.ProjectStatus.ToListAsync();
            foreach (var d in TenantSeedCatalog.ProjectStatuses)
            {
                var match = existing.FirstOrDefault(x => string.Equals(x.Name, d.Name, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    var created = new ProjectStatusEntity();
                    created.Update(d.Name, d.Color, d.SortOrder, d.IsVisible);
                    created.SetHitRateSettings(d.CountsAsSubmittedBid, d.CountsAsWonBid, d.CountsAsLostBid);
                    created.SetIsDefault(d.IsDefault);
                    ctx.ProjectStatus.Add(created);
                    continue;
                }

                match.Update(d.Name, d.Color, d.SortOrder, d.IsVisible);
                match.SetHitRateSettings(d.CountsAsSubmittedBid, d.CountsAsWonBid, d.CountsAsLostBid);
                if (d.IsDefault && !existing.Any(x => x.IsDefault))
                    match.SetIsDefault(true);
            }
        }

        // The five "ordered list" lookups share an identical shape, so a single generic upsert covers them.
        private static async Task SeedProjectLookupsAsync(ShardingSingleDbContext ctx)
        {
            SeedOrderedLookups(ctx.Compensations, await ctx.Compensations.ToListAsync(),
                TenantSeedCatalog.Compensations.Select(c => (c.Name, c.Color, c.SortOrder, c.IsVisible, c.IsDefault)),
                (e, v) => e.SetIsDefault(v), e => e.IsDefault);

            SeedOrderedLookups(ctx.Contracts, await ctx.Contracts.ToListAsync(),
                TenantSeedCatalog.Contracts.Select(c => (c.Name, c.Color, c.SortOrder, c.IsVisible, c.IsDefault)),
                (e, v) => e.SetIsDefault(v), e => e.IsDefault);

            SeedOrderedLookups(ctx.ProcurementProcedure, await ctx.ProcurementProcedure.ToListAsync(),
                TenantSeedCatalog.ProcurementProcedures.Select(c => (c.Name, c.Color, c.SortOrder, c.IsVisible, c.IsDefault)),
                (e, v) => e.SetIsDefault(v), e => e.IsDefault);

            SeedOrderedLookups(ctx.ProcurementMethod, await ctx.ProcurementMethod.ToListAsync(),
                TenantSeedCatalog.ProcurementMethods.Select(c => (c.Name, c.Color, c.SortOrder, c.IsVisible, c.IsDefault)),
                (e, v) => e.SetIsDefault(v), e => e.IsDefault);

            SeedOrderedLookups(ctx.CalcProjectType, await ctx.CalcProjectType.ToListAsync(),
                TenantSeedCatalog.ProjectTypes.Select(c => (c.Name, c.Color, c.SortOrder, c.IsVisible, c.IsDefault)),
                (e, v) => e.SetIsDefault(v), e => e.IsDefault);
        }

        private static void SeedOrderedLookups<TEntity>(
            Microsoft.EntityFrameworkCore.DbSet<TEntity> set,
            List<TEntity> existing,
            IEnumerable<(string Name, string Color, int SortOrder, bool IsVisible, bool IsDefault)> defaults,
            Action<TEntity, bool> setIsDefault,
            Func<TEntity, bool> isDefault)
            where TEntity : OrderedListEntity, new()
        {
            var anyDefaultExists = existing.Any(isDefault);
            foreach (var d in defaults)
            {
                var match = existing.FirstOrDefault(x => string.Equals(x.Name, d.Name, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    var entity = new TEntity();
                    entity.Update(d.Name, d.Color, d.SortOrder, d.IsVisible);
                    setIsDefault(entity, d.IsDefault);
                    set.Add(entity);
                    continue;
                }

                match.Update(d.Name, d.Color, d.SortOrder, d.IsVisible);
                if (d.IsDefault && !anyDefaultExists)
                    setIsDefault(match, true);
            }
        }

        public async Task<List<GetTenantsDTO>> GetAsync()
        {
            if (!await HasAnyRoleAsync(PMRolesConst.APP.AdminManger))
                return [];

            using var appContext = ContextFactory.CreateDbContext();
            var tenants = await appContext.Tenants
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

            // User counts + block status, aggregated per tenant in a single query.
            var now = DateTimeOffset.UtcNow;
            var aggregates = await appContext.Users
                .AsNoTracking()
                .Where(u => u.TenantId != null)
                .GroupBy(u => u.TenantId!.Value)
                .Select(g => new
                {
                    TenantId = g.Key,
                    Total = g.Count(),
                    Locked = g.Count(u => u.LockoutEnabled && u.LockoutEnd != null && u.LockoutEnd > now)
                })
                .ToListAsync();

            var aggregateByTenant = aggregates.ToDictionary(a => a.TenantId);
            foreach (var tenant in tenants)
            {
                if (!aggregateByTenant.TryGetValue(tenant.Id, out var agg))
                    continue;

                tenant.UsersCount = agg.Total;
                tenant.IsBlocked = agg.Total > 0 && agg.Locked == agg.Total;
            }

            return tenants;
        }

        public async Task<Dictionary<int, string>> GetDepartmentNamesAsync(int tenantId)
        {
            if (!await HasAnyRoleAsync(PMRolesConst.APP.AdminManger))
                return [];

            if (tenantId <= 0 || !await TenantExistsAsync(tenantId))
                return [];

            await using var dataAccess = await CreateTenantDbContextAsync(tenantId);
            return await dataAccess.Department
                .AsNoTracking()
                .ToDictionaryAsync(d => d.Id, d => d.Name);
        }

        public async Task<(bool Ok, string? Error)> TestDatabaseConnectionAsync(string connectionString)
        {
            if (!await HasAnyRoleAsync(PMRolesConst.APP.AdminManger))
                return (false, "Not authorized.");

            if (string.IsNullOrWhiteSpace(connectionString))
                return (false, "Connection string is empty.");

            try
            {
                // Keep the probe short so a bad/unreachable target fails fast.
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
                {
                    ConnectTimeout = 5
                };

                await using var connection = new Microsoft.Data.SqlClient.SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
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

        public async Task<Dictionary<string, string?>> GetUserRolesMapAsync(int? tenantId)
        {
            if (!await CanManageUserScopeAsync(tenantId, writeOperation: false))
                return [];

            using var appContext = ContextFactory.CreateDbContext();

            var usersQuery = appContext.Users.AsNoTracking();
            usersQuery = tenantId.HasValue && tenantId > 0
                ? usersQuery.Where(u => u.TenantId == tenantId)
                : usersQuery.Where(u => !u.TenantId.HasValue);

            // Single round-trip: left-join the Identity role tables instead of one GetRolesAsync per user.
            var pairs = await (from u in usersQuery
                               join ur in appContext.UserRoles.AsNoTracking() on u.Id equals ur.UserId into urj
                               from ur in urj.DefaultIfEmpty()
                               join r in appContext.Roles.AsNoTracking() on ur.RoleId equals r.Id into rj
                               from r in rj.DefaultIfEmpty()
                               select new { u.Id, RoleName = r != null ? r.Name : null })
                              .ToListAsync();

            // One role per user is enforced elsewhere; keep the first non-null if data ever drifts.
            return pairs
                .GroupBy(x => x.Id, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).FirstOrDefault(n => n != null), StringComparer.Ordinal);
        }

        public async Task<bool> ResetUserPasswordAsync(string userId, int? tenantId)
        {
            if (!await CanManageUserScopeAsync(tenantId, writeOperation: true))
                return false;

            if (string.IsNullOrWhiteSpace(userId))
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.TenantId != tenantId)
                return false;

            var password = IdentityUserSyncHelper.GenerateTemporaryPassword();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, password);
            if (!result.Succeeded)
                return false;

            await _userManager.UpdateSecurityStampAsync(user);

            try
            {
                await _accountNotificationEmailSender.SendUserCreatedAsync(
                    user.Email ?? string.Empty,
                    $"{user.Firstname} {user.Lastname}".Trim(),
                    password,
                    passwordWasGenerated: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Password for {Email} was reset but the notification email could not be sent.", user.Email);
            }

            _logger.LogInformation(
                "AUDIT password reset. TenantId={TenantId} TargetUserId={TargetUserId} By={ActorEmail}",
                tenantId, userId, await CurrentActorAsync());
            return true;
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

            // Delete all tenant-scoped rows inside a single transaction so a mid-way failure
            // can't leave the tenant database half-wiped. (EnableRetryOnFailure needs the
            // execution strategy to own the transaction.)
            var strategy = dataAccess.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                dataAccess.ChangeTracker.Clear();
                await using var transaction = await dataAccess.Database.BeginTransactionAsync();

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

                await transaction.CommitAsync();
            });

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

            _logger.LogInformation(
                "AUDIT tenant deleted. TenantId={TenantId} IdentityUsersRemoved={IdentityUsersRemoved} By={ActorEmail}",
                TenantId, users.Count, await CurrentActorAsync());
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

                // Don't leave a department pointing at a deleted head user.
                await dataAccess.Department
                    .Where(d => d.HeadUserId == localUser.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.HeadUserId, (int?)null));

                dataAccess.User.Remove(localUser);
                await dataAccess.SaveChangesAsync();
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "AUDIT user deleted. TenantId={TenantId} TargetUserId={TargetUserId} By={ActorEmail}",
                    tenantId, id, await CurrentActorAsync());
            }

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

            _logger.LogInformation(
                "AUDIT tenant {Action}. TenantId={TenantId} AffectedUsers={AffectedUsers} By={ActorEmail}",
                block ? "blocked" : "unblocked", TenantId, users.Count, await CurrentActorAsync());
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

            _logger.LogInformation(
                "AUDIT app user {Action}. TargetUserId={TargetUserId} By={ActorEmail}",
                block ? "blocked" : "unblocked", userid, await CurrentActorAsync());
            return true;
        }

        public async Task<bool> SetTenantUserLockoutAsync(string userId, int tenantId, bool block)
        {
            if (!await HasAnyRoleAsync(PMRolesConst.APP.AdminManger))
                return false;

            if (string.IsNullOrWhiteSpace(userId) || tenantId <= 0)
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.TenantId != tenantId)
                return false;

            // Don't let an actor lock themselves out, and keep at least one active tenant admin.
            if (block && await IsCurrentUserAsync(user.Id))
                return false;

            if (block && await IsLastTenantAdminAsync(user, tenantId))
                return false;

            user.LockoutEnabled = block;
            user.LockoutStart = block ? DateTimeOffset.UtcNow : null;
            user.LockoutEnd = block ? DateTimeOffset.UtcNow.AddYears(100) : null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return false;

            // Invalidate active sessions so a block takes effect immediately.
            await _userManager.UpdateSecurityStampAsync(user);

            _logger.LogInformation(
                "AUDIT tenant user {Action}. TenantId={TenantId} TargetUserId={TargetUserId} By={ActorEmail}",
                block ? "blocked" : "unblocked", tenantId, userId, await CurrentActorAsync());
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
