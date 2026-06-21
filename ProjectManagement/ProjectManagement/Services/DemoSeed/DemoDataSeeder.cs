using AuthPermissions;
using AuthPermissions.Context;
using AuthPermissions.Services;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using Domain.Entities.Project;
using Domain.Entities.ResourceType;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Interceptors;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Organisation;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.DTO.ResourceType;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Services.DemoSeed;

/// <summary>
/// Dev/test demo data generator. Creates three companies/tenants (each in its own physical database)
/// with departments, users, per-company dropdown values, organisations, resources, projects and
/// calculations. Everything is matched by stable name/code so re-running is idempotent.
/// </summary>
public sealed class DemoDataSeeder(
    IConfiguration configuration,
    AuthPermissionDbContext authDb,
    UserManager<ApplicationUser> userManager,
    ITenantConnectionStringStore connectionStore,
    ILogger<DemoDataSeeder> logger)
{
    public async Task<string> SeedAsync(CancellationToken ct = default)
    {
        var tenantsTouched = 0;
        foreach (var company in DemoSeedCatalog.Companies)
        {
            ct.ThrowIfCancellationRequested();
            var (tenantId, connectionString) = await EnsureTenantAndDatabaseAsync(company, ct);
            await using var db = CreateTenantContext(connectionString, tenantId);

            await SeedTenantAsync(db, company, tenantId, ct);
            tenantsTouched++;
        }

        // Refresh the running app's tenant→connection cache so the new tenants are immediately usable.
        await connectionStore.ReloadAsync(ct);
        return $"Seeded {tenantsTouched} demo tenant(s): {string.Join(", ", DemoSeedCatalog.Companies.Select(c => c.Name))}.";
    }

    // ── Tenant + physical database provisioning ────────────────────────────────
    private async Task<(int TenantId, string ConnectionString)> EnsureTenantAndDatabaseAsync(DemoCompany company, CancellationToken ct)
    {
        var dbName = $"PM_Demo_{company.Slug}";
        var connectionString = BuildTenantConnectionString(dbName);

        // Idempotent: reuse an existing TenantDatabase row (by name) or create one.
        var tenantDb = await authDb.TenantDatabase.FirstOrDefaultAsync(x => x.Name == dbName, ct);
        if (tenantDb is null)
        {
            tenantDb = new AuthPermissions.Entity.TenantDatabaseEntity
            {
                Name = dbName,
                ConnectionString = connectionString,
            };
            authDb.TenantDatabase.Add(tenantDb);
            await authDb.SaveChangesAsync(ct);
        }
        else if (tenantDb.ConnectionString != connectionString)
        {
            tenantDb.ConnectionString = connectionString;
            await authDb.SaveChangesAsync(ct);
        }

        var tenant = await authDb.Tenants.FirstOrDefaultAsync(x => x.Name == company.Name, ct);
        if (tenant is null)
        {
            tenant = new AuthPermissions.Entity.TenantEntity
            {
                Name = company.Name,
                TenantDBId = tenantDb.Id,
                MaxUsers = 100,
                DateExpire = DateTime.UtcNow.AddYears(5),
                Note = "DEMO",
            };
            authDb.Tenants.Add(tenant);
            await authDb.SaveChangesAsync(ct);
        }
        else if (tenant.TenantDBId != tenantDb.Id)
        {
            tenant.TenantDBId = tenantDb.Id;
            await authDb.SaveChangesAsync(ct);
        }

        // Ensure the physical tenant database exists and is on the latest schema.
        await using (var schema = CreateTenantContext(connectionString, tenant.Id))
        {
            await schema.Database.MigrateAsync(ct);
        }

        logger.LogInformation("Demo tenant ready: {Tenant} (Id={TenantId}, DB={Db}).", company.Name, tenant.Id, dbName);
        return (tenant.Id, connectionString);
    }

    private string BuildTenantConnectionString(string databaseName)
    {
        var baseConn = configuration.GetConnectionString("AuthPermissionsConnection")
            ?? throw new InvalidOperationException("AuthPermissionsConnection is not configured.");
        var builder = new SqlConnectionStringBuilder(baseConn)
        {
            InitialCatalog = databaseName,
            MultipleActiveResultSets = true,
        };
        return builder.ConnectionString;
    }

    private static ShardingSingleDbContext CreateTenantContext(string connectionString, int tenantId)
    {
        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                sql.CommandTimeout(60);
            })
            .AddInterceptors(new TenantAuditSaveChangesInterceptor())
            .Options;

        return new ShardingSingleDbContext(options) { TenantId = tenantId };
    }

    // ── Per-tenant content ─────────────────────────────────────────────────────
    private async Task SeedTenantAsync(ShardingSingleDbContext db, DemoCompany company, int tenantId, CancellationToken ct)
    {
        await SeedLookupsAsync(db, company, ct);
        await SeedAccountsAsync(db, company, ct);
        var resourceTypeByKind = await SeedResourceTypesAsync(db, company, ct);
        await SeedResourceSortsAsync(db, company, resourceTypeByKind, ct);
        await SeedDepartmentsAsync(db, ct);
        await SeedOrganisationsAsync(db, company, ct);
        var owner = await SeedUsersAsync(db, company, tenantId, ct);
        if (owner is not null)
        {
            db.CurrentUserId = owner.Value;
            // Resources must belong to a task (CK_Resources_Task_Positive), so the company's resource
            // register is materialized as calculation rows rather than a standalone library.
            await SeedProjectsAsync(db, company, owner.Value, ct);
            await SeedProjectSharesAsync(db, ct);
        }
    }

    private static async Task SeedLookupsAsync(ShardingSingleDbContext db, DemoCompany company, CancellationToken ct)
    {
        // Project statuses (own entity with a single default).
        var projectStatuses = await db.ProjectStatus.ToListAsync(ct);
        foreach (var d in company.ProjectStatuses)
        {
            var match = projectStatuses.FirstOrDefault(x => Eq(x.Name, d.Name));
            if (match is null)
            {
                var e = new ProjectStatusEntity();
                e.Update(d.Name, d.Color, d.Sort, true);
                e.SetIsDefault(d.IsDefault);
                db.ProjectStatus.Add(e);
            }
            else
            {
                match.Update(d.Name, d.Color, d.Sort, true);
            }
        }

        // Calculation statuses.
        var calcStatuses = await db.CalculationStatus.ToListAsync(ct);
        foreach (var d in company.CalcStatuses)
        {
            var match = calcStatuses.FirstOrDefault(x => Eq(x.Name, d.Name));
            if (match is null)
            {
                var e = new StatusEntity();
                e.Update(d.Name, d.Color, d.Sort, true);
                e.SetIsDefault(d.IsDefault);
                db.CalculationStatus.Add(e);
            }
            else
            {
                match.Update(d.Name, d.Color, d.Sort, true);
            }
        }

        await UpsertOrderedAsync(db.CalcProjectType, company.ProjectTypes, (e, v) => e.SetIsDefault(v), e => e.IsDefault, ct);
        await UpsertOrderedAsync(db.ProcurementMethod, company.ProcurementForms, (e, v) => e.SetIsDefault(v), e => e.IsDefault, ct);
        await UpsertOrderedAsync(db.ProcurementProcedure, company.ProcurementProcedures, (e, v) => e.SetIsDefault(v), e => e.IsDefault, ct);
        await UpsertOrderedAsync(db.Contracts, company.ContractForms, (e, v) => e.SetIsDefault(v), e => e.IsDefault, ct);
        await UpsertOrderedAsync(db.Compensations, company.Compensations, (e, v) => e.SetIsDefault(v), e => e.IsDefault, ct);

        await db.SaveChangesAsync(ct);
    }

    private static async Task UpsertOrderedAsync<TEntity>(
        DbSet<TEntity> set, DemoLookup[] defaults,
        Action<TEntity, bool> setIsDefault, Func<TEntity, bool> isDefault, CancellationToken ct)
        where TEntity : Domain.Entities.Base.OrderedListEntity, new()
    {
        var existing = await set.ToListAsync(ct);
        var anyDefault = existing.Any(isDefault);
        foreach (var d in defaults)
        {
            var match = existing.FirstOrDefault(x => Eq(x.Name, d.Name));
            if (match is null)
            {
                var e = new TEntity();
                e.Update(d.Name, d.Color, d.Sort, true);
                if (d.IsDefault && !anyDefault)
                    setIsDefault(e, true);
                set.Add(e);
            }
            else
            {
                match.Update(d.Name, d.Color, d.Sort, true);
            }
        }
    }

    private static async Task SeedAccountsAsync(ShardingSingleDbContext db, DemoCompany company, CancellationToken ct)
    {
        var groups = await db.AccountGroup.ToListAsync(ct);
        foreach (var g in company.AccountGroups)
        {
            if (!groups.Any(x => Eq(x.Name, g)))
                db.AccountGroup.Add(new AccountGroupEntity(g));
        }
        await db.SaveChangesAsync(ct);

        var groupByName = await db.AccountGroup.ToDictionaryAsync(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase, ct);
        var existing = await db.Accounts.ToListAsync(ct);
        foreach (var a in company.Accounts)
        {
            if (!groupByName.TryGetValue(a.Group, out var groupId))
                continue;

            var match = existing.FirstOrDefault(x => Eq(x.Code, a.Code));
            if (match is null)
                db.Accounts.Add(new AccountEntity(a.Code, a.Name, groupId, true, new AccountData()));
            else
                match.Update(a.Code, a.Name, groupId, true, match.Metadata);
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<ResourceTypesEnum, int>> SeedResourceTypesAsync(
        ShardingSingleDbContext db, DemoCompany company, CancellationToken ct)
    {
        var existing = await db.ResourceTypes.ToListAsync(ct);
        foreach (var rt in company.ResourceTypes)
        {
            var dto = new PostResourceTypeDTO { Name = rt.Name, IsVisible = true, Type = rt.Kind, Order = rt.Sort };
            var match = existing.FirstOrDefault(x => x.Kind == rt.Kind);
            if (match is null)
                db.ResourceTypes.Add(ResourceTypeEntity.Create(dto, rt.Sort));
            else
            {
                match.Update(dto);
                match.UpdateOrder(rt.Sort);
            }
        }
        await db.SaveChangesAsync(ct);
        return await db.ResourceTypes.ToDictionaryAsync(x => x.Kind, x => x.Id, ct);
    }

    private static async Task SeedResourceSortsAsync(
        ShardingSingleDbContext db, DemoCompany company,
        Dictionary<ResourceTypesEnum, int> resourceTypeByKind, CancellationToken ct)
    {
        // ResourceSort requires a ResourceType FK; attach every demo sort to the first resource type.
        // (The app's sort list is per-resource-type; for demo data one anchor type is enough.)
        var anchorTypeId = resourceTypeByKind.Values.FirstOrDefault();
        if (anchorTypeId == 0)
            return;

        var existing = await db.ResourceSorts.ToListAsync(ct);
        foreach (var s in company.ResourceSorts)
        {
            if (existing.Any(x => Eq(x.Name, s.Name)))
                continue;

            var dto = new PostResourceSortDTO { Name = s.Name, IsVisible = true, ResourceTypeId = anchorTypeId };
            db.ResourceSorts.Add(ResourceSortEntity.Create(dto, anchorTypeId, s.Sort));
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedDepartmentsAsync(ShardingSingleDbContext db, CancellationToken ct)
    {
        var existing = await db.Department.Select(x => x.Name).ToListAsync(ct);
        foreach (var name in DemoSeedCatalog.Departments)
        {
            if (!existing.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
                db.Department.Add(DepartmentEntity.Create(new DepartmentBase { Name = name, Description = $"Demo {name}" }));
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedOrganisationsAsync(ShardingSingleDbContext db, DemoCompany company, CancellationToken ct)
    {
        // Categories (Kund/Leverantör/...) and a single "Företag" type.
        var categories = await db.OrganisationCategory.ToListAsync(ct);
        foreach (var c in DemoSeedCatalog.OrgCategories)
        {
            if (!categories.Any(x => Eq(x.Name, c)))
                db.OrganisationCategory.Add(OrganisationCategoryEntity.Create(new PostOrganisationCategoryDTO { Name = c }));
        }
        await db.SaveChangesAsync(ct);
        var categoryByName = await db.OrganisationCategory.ToDictionaryAsync(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase, ct);

        var existing = await db.Organisation.ToListAsync(ct);
        foreach (var o in company.Organisations)
        {
            if (existing.Any(x => Eq(x.Name, o.Name)))
                continue;
            if (!categoryByName.TryGetValue(o.Category, out var categoryId))
                continue;

            var dto = new PostOrganisationDTO
            {
                Name = o.Name,
                CategoryId = categoryId,
                IsVisible = true,
                IDNumber = o.OrgNumber,
                Email = o.Email,
                Phone = o.Phone,
            };
            db.Organisation.Add(OrganisationEntity.Create(dto));
        }
        await db.SaveChangesAsync(ct);
    }

    // ── Users ──────────────────────────────────────────────────────────────────
    // Returns a local user id to own demo projects/calcs (the tenant admin), or null on failure.
    private async Task<int?> SeedUsersAsync(ShardingSingleDbContext db, DemoCompany company, int tenantId, CancellationToken ct)
    {
        var departments = await db.Department.OrderBy(x => x.Id).ToListAsync(ct);
        if (departments.Count < 3)
            return null;

        int Dept(string name) => departments.First(d => Eq(d.Name, name)).Id;

        // Admin owns the demo content; lives in all departments conceptually (no single home dept).
        var adminLocalId = await EnsureUserAsync(db, company, tenantId,
            $"{company.Slug}.admin@{DemoSeedCatalog.AdminEmailDomain}",
            $"{TitleCase(company.Slug)} Admin", PMRolesConst.Tenant.Admin, departmentId: null, ct);

        // Department distribution per the spec (single home department; multi-dept access is a model
        // limitation noted in the seeder docs — UserEntity has one DepartmentId).
        var userDepts = new[]
        {
            Dept("Kalkyl"), Dept("Kalkyl"), Dept("Kalkyl"),            // 01-03 Kalkyl
            Dept("Produktion"), Dept("Produktion"), Dept("Produktion"),// 04-06 Produktion
            Dept("Kalkyl"),                                            // 07 Kalkyl+Produktion → Kalkyl
            Dept("Produktion"),                                        // 08 Produktion+Ledning → Produktion
            Dept("Kalkyl"),                                            // 09 all three → Kalkyl
        };
        var viewerDepts = new[]
        {
            Dept("Produktion"), Dept("Produktion"), Dept("Produktion"),// 01-03 Produktion
            Dept("Kalkyl"), Dept("Kalkyl"), Dept("Kalkyl"),            // 04-06 Kalkyl
            Dept("Kalkyl"),                                            // 07 Kalkyl+Produktion → Kalkyl
            Dept("Produktion"),                                        // 08 Produktion+Ledning → Produktion
            Dept("Ledning"),                                           // 09 all three → Ledning
        };

        for (var i = 1; i <= 9; i++)
        {
            await EnsureUserAsync(db, company, tenantId,
                $"{company.Slug}.user{i:00}@{DemoSeedCatalog.AdminEmailDomain}",
                $"{TitleCase(company.Slug)} Användare {i:00}", PMRolesConst.Tenant.Manger, userDepts[i - 1], ct);

            await EnsureUserAsync(db, company, tenantId,
                $"{company.Slug}.viewer{i:00}@{DemoSeedCatalog.AdminEmailDomain}",
                $"{TitleCase(company.Slug)} Visare {i:00}", PMRolesConst.Tenant.Viewer, viewerDepts[i - 1], ct);
        }

        return adminLocalId;
    }

    private async Task<int?> EnsureUserAsync(
        ShardingSingleDbContext db, DemoCompany company, int tenantId,
        string email, string displayName, string role, int? departmentId, CancellationToken ct)
    {
        var (firstName, lastName) = SplitName(displayName);

        // Local tenant-side user (idempotent by email).
        var localUser = await db.User.FirstOrDefaultAsync(x => x.Email == email, ct);
        if (localUser is null)
        {
            localUser = UserEntity.Create(tenantId, email, email, departmentId, firstName, lastName);
            db.User.Add(localUser);
            await db.SaveChangesAsync(ct);
        }
        else if (localUser.DepartmentId != departmentId)
        {
            localUser.UpdateProfile(firstName, lastName, departmentId);
            await db.SaveChangesAsync(ct);
        }

        // Identity user (idempotent by email).
        var identityUser = await userManager.FindByEmailAsync(email);
        if (identityUser is null)
        {
            identityUser = new ApplicationUser { EmailConfirmed = true };
            IdentityUserSyncHelper.ApplyToIdentityUser(
                identityUser, email, email, tenantId, departmentId, localUser.Id,
                firstName, lastName, phoneNumber: null, phoneNumberConfirmed: false,
                lockoutEnabled: false, lockoutStart: null, lockoutEnd: null, isAppUser: false);

            var created = await userManager.CreateAsync(identityUser, DemoSeedCatalog.SharedPassword);
            if (!created.Succeeded)
            {
                logger.LogWarning("Demo user {Email} could not be created: {Errors}",
                    email, string.Join("; ", created.Errors.Select(e => e.Description)));
                return null;
            }

            await IdentityUserSyncHelper.EnsureSingleRoleAsync(userManager, identityUser, role);

            localUser.SetExternalAuthId(identityUser.Id);
            await db.SaveChangesAsync(ct);
            identityUser.UserId = localUser.Id;
            await userManager.UpdateAsync(identityUser);
        }
        else if (string.IsNullOrEmpty(localUser.ExternalAuthId))
        {
            // Re-link a pre-existing identity user to the local row.
            localUser.SetExternalAuthId(identityUser.Id);
            await db.SaveChangesAsync(ct);
        }

        return localUser.Id;
    }

    // ── Projects, calculations and rows ─────────────────────────────────────────
    private static async Task SeedProjectsAsync(ShardingSingleDbContext db, DemoCompany company, int ownerId, CancellationToken ct)
    {
        var departments = await db.Department.OrderBy(x => x.Id).ToListAsync(ct);
        var projectStatusId = await db.ProjectStatus.OrderBy(x => x.SortOrder).Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
        var typeId = await db.CalcProjectType.OrderBy(x => x.SortOrder).Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
        var calcStatusId = await db.CalculationStatus.OrderBy(x => x.SortOrder).Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
        var lockStatusId = await db.CalculationStatus.OrderByDescending(x => x.SortOrder).Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
        var taskStatusId = await db.TaskStatus.OrderBy(x => x.SortOrder).Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
        var resourceStatusId = await db.ResourceStatus.OrderBy(x => x.SortOrder).Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
        var resourceTypeByKind = await db.ResourceTypes.ToDictionaryAsync(x => x.Kind, x => x.Id, ct);
        var accountByCode = await db.Accounts.ToDictionaryAsync(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase, ct);
        var sortByName = await db.ResourceSorts.ToDictionaryAsync(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase, ct);

        // Continue project numbering after any existing P-<n> codes (unique per tenant).
        var nextProjectNumber = 1;
        foreach (var code in await db.Projects.Select(p => p.Code).ToListAsync(ct))
        {
            if (!string.IsNullOrEmpty(code) && code.StartsWith("P-", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(code.AsSpan(2), out var n) && n >= nextProjectNumber)
                nextProjectNumber = n + 1;
        }

        // One "Demoprojekt" folder per department (idempotent marker), 4 projects each.
        var seededDepartmentIds = (await db.Folders
            .Where(x => x.Name == DemoSeedCatalog.DemoFolders[0])
            .Select(x => x.DepartmentId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

        var projectIndex = 0;
        foreach (var department in departments)
        {
            if (seededDepartmentIds.Contains(department.Id))
                continue;

            var folder = new FolderEntity(DemoSeedCatalog.DemoFolders[0], "#2563EB", department.Id, ownerId, 100);
            db.Folders.Add(folder);
            await db.SaveChangesAsync(ct);

            for (var i = 0; i < 4; i++)
            {
                var dto = new PostProjectDTO
                {
                    Name = $"Demoprojekt {company.Slug} {projectIndex + 1:00}",
                    Code = $"P-{nextProjectNumber:00}",
                    FolderId = folder.Id,
                    StatusId = projectStatusId,
                    TypeId = typeId,
                };
                var project = ProjectEntity.Create(dto, folder.Id, ownerId, (i + 1) * 100);
                db.Projects.Add(project);
                await db.SaveChangesAsync(ct);
                nextProjectNumber++;
                projectIndex++;

                await SeedCalculationsForProjectAsync(
                    db, company, project.Id, department.Id, ownerId,
                    calcStatusId, lockStatusId, typeId, taskStatusId, resourceStatusId,
                    resourceTypeByKind, accountByCode, sortByName, ct);
            }
        }
    }

    private static async Task SeedCalculationsForProjectAsync(
        ShardingSingleDbContext db, DemoCompany company, Guid projectId, int departmentId, int ownerId,
        int? calcStatusId, int? lockStatusId, int? typeId, int? taskStatusId, int? resourceStatusId,
        Dictionary<ResourceTypesEnum, int> resourceTypeByKind,
        Dictionary<string, int> accountByCode, Dictionary<string, int> sortByName, CancellationToken ct)
    {
        // 3 calculations per project: a normal draft, a locked/approved one, and a private one.
        for (var c = 0; c < 3; c++)
        {
            var isLocked = c == 1;
            var isPrivate = c == 2;
            var calc = new CalculationEntity();
            calc.AssignDepartment(departmentId);
            calc.AssignToProject(projectId);
            calc.Update(new CalculationPostDTO
            {
                Name = isPrivate ? $"Privat kalkyl {c + 1:00}" : $"Kalkyl {c + 1:00}",
                Code = $"K-{c + 1:00}",
                StatusId = isLocked ? lockStatusId : calcStatusId,
                TypeId = typeId,
                Order = (c + 1) * 100,
                IsPrivate = isPrivate,
            });
            db.Calculations.Add(calc);
            await db.SaveChangesAsync(ct);

            // Rows: 12 tasks each carrying one resource (mix of accounts/types/sorts for import tests).
            var taskSort = 100;
            for (var t = 0; t < 12; t++)
            {
                var task = TaskEntity.Create(calc.Id, new TaskPostDTO
                {
                    Name = $"Post {t + 1:00}",
                    Unit = "st",
                    Quantity = (t + 1) * 2m,
                    StatusId = taskStatusId,
                }, taskSort);
                db.Tasks.Add(task);
                await db.SaveChangesAsync(ct);

                var r = company.Resources[t % company.Resources.Length];
                resourceTypeByKind.TryGetValue(r.Kind, out var resTypeId);
                accountByCode.TryGetValue(r.AccountCode, out var accId);
                sortByName.TryGetValue(r.Sort, out var sortId);

                var dto = new ResourcePostDTO
                {
                    Name = r.Name,
                    ResType = r.Kind,
                    Unit = r.Unit,
                    Quantity = (t + 1) * 2m,
                    SortOrder = 100,
                    StatusId = resourceStatusId,
                    ResourceTypeId = resTypeId > 0 ? resTypeId : null,
                    AccountId = accId > 0 ? accId : null,
                    ResourceSortId = sortId > 0 ? sortId : null,
                    Data = new ResourceMetadata { Cost = r.Cost },
                };
                db.Resources.Add(ResourceEntity.Create(dto, 100, parentTaskId: task.Id));
                taskSort += 100;
            }
            await db.SaveChangesAsync(ct);

            if (isLocked)
            {
                calc.ApproveAndLock(ownerId, "DemoSeed");
                await db.SaveChangesAsync(ct);
            }
        }
    }

    // ── Project sharing ─────────────────────────────────────────────────────────
    // Shares a few demo projects with individual viewers and with whole departments. Only the
    // non-private calculations of each project are shared (private calcs are never exposed).
    private static async Task SeedProjectSharesAsync(ShardingSingleDbContext db, CancellationToken ct)
    {
        var projects = await db.Projects
            .Where(p => p.Code != null && p.Code.StartsWith("P-"))
            .OrderBy(p => p.Code)
            .Select(p => p.Id)
            .ToListAsync(ct);
        if (projects.Count == 0)
            return;

        var viewers = await db.User
            .Where(u => u.Email.Contains(".viewer"))
            .OrderBy(u => u.Email)
            .Select(u => u.Id)
            .ToListAsync(ct);
        var usersRole = await db.User
            .Where(u => u.Email.Contains(".user"))
            .OrderBy(u => u.Email)
            .Select(u => u.Id)
            .ToListAsync(ct);
        var deptByName = await db.Department.ToDictionaryAsync(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase, ct);

        var existingUserShares = (await db.ProjectShare
            .Where(s => s.SharedWithUserId != null)
            .Select(s => new { s.ProjectId, s.SharedWithUserId })
            .ToListAsync(ct))
            .Select(s => (s.ProjectId, s.SharedWithUserId!.Value))
            .ToHashSet();
        var existingDeptShares = (await db.ProjectShare
            .Where(s => s.DepartmentId != null)
            .Select(s => new { s.ProjectId, s.DepartmentId })
            .ToListAsync(ct))
            .Select(s => (s.ProjectId, s.DepartmentId!.Value))
            .ToHashSet();

        async Task<List<int>> NonPrivateCalcIds(Guid projectId) => await db.Calculations
            .Where(c => c.ProjectId == projectId && !c.IsPrivate)
            .Select(c => c.Id)
            .ToListAsync(ct);

        // 3 projects → individual viewers (only a subset of the calcs).
        for (var i = 0; i < 3 && i < projects.Count && i < viewers.Count; i++)
        {
            var projectId = projects[i];
            var viewerId = viewers[i];
            if (existingUserShares.Contains((projectId, viewerId)))
                continue;

            var calcIds = await NonPrivateCalcIds(projectId);
            var share = ProjectShareEntity.ForUser(projectId, viewerId, PMRolesConst.Tenant.Viewer);
            share.ReplaceCalculations(calcIds.Take(Math.Max(1, calcIds.Count - 1)));
            db.ProjectShare.Add(share);
        }

        // 2 projects → Produktion department, 1 project → Ledning department.
        if (deptByName.TryGetValue("Produktion", out var produktionId))
        {
            foreach (var projectId in projects.Skip(3).Take(2))
            {
                if (existingDeptShares.Contains((projectId, produktionId)))
                    continue;
                var share = ProjectShareEntity.ForDepartment(projectId, produktionId, PMRolesConst.Tenant.Viewer);
                share.ReplaceCalculations(await NonPrivateCalcIds(projectId));
                db.ProjectShare.Add(share);
            }
        }
        if (deptByName.TryGetValue("Ledning", out var ledningId))
        {
            foreach (var projectId in projects.Skip(5).Take(1))
            {
                if (existingDeptShares.Contains((projectId, ledningId)))
                    continue;
                var share = ProjectShareEntity.ForDepartment(projectId, ledningId, PMRolesConst.Tenant.Viewer);
                share.ReplaceCalculations(await NonPrivateCalcIds(projectId));
                db.ProjectShare.Add(share);
            }
        }

        // 2 projects → individual "Användare" role users.
        for (var i = 0; i < 2 && (6 + i) < projects.Count && i < usersRole.Count; i++)
        {
            var projectId = projects[6 + i];
            var userId = usersRole[i];
            if (existingUserShares.Contains((projectId, userId)))
                continue;
            var calcIds = await NonPrivateCalcIds(projectId);
            var share = ProjectShareEntity.ForUser(projectId, userId, PMRolesConst.Tenant.Manger);
            share.ReplaceCalculations(calcIds);
            db.ProjectShare.Add(share);
        }

        await db.SaveChangesAsync(ct);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────
    private static bool Eq(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static string TitleCase(string slug) => string.IsNullOrEmpty(slug)
        ? slug
        : char.ToUpperInvariant(slug[0]) + slug[1..];

    private static (string First, string Last) SplitName(string displayName)
    {
        var parts = displayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? (parts[0], parts[1]) : (displayName, string.Empty);
    }
}
