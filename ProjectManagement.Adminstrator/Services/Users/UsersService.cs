using AuthPermissions.Entity;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using Domain.Entities.ResourceType;
using Domain.Entities.Users;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Persistence.Context;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.DTO.ResourceType;
using ProjectManagement.Shared.DTO.Identity;
using ProjectManagement.Shared.DTO.Tenant;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Models.Account;

namespace ProjectManagement.Adminstrator.Services.Users
{
    public class UsersService(UserManager<ApplicationUser> _userManager,
        IServiceScopeFactory _scopeFactory,
          IDbContextFactory<ApplicationDbContext> ContextFactory,
          ILogger<UsersService> _logger
       ) : IUsersService
    {

        public async Task<List<GetTenantsDTO>> GetAsync()
        {
            using var _appContext = ContextFactory.CreateDbContext();
            return await _appContext.Tenants.Select(x => new GetTenantsDTO()
            {
                Name = x.Name,
                DateExpire = x.DateExpire,
                DB = x.TenantDB.Name,
                Id = x.Id,
            }).ToListAsync();
        }

        public async Task<TenantEntity> GetByIdAsync(int id)
        {
            using var _appContext = ContextFactory.CreateDbContext();
            return await _appContext.Tenants.FirstOrDefaultAsync(x => x.Id == id) ?? new TenantEntity();
        }
        public async Task<int> CreateAsync(TenantEntity tenant)
        {
            using var _appContext = ContextFactory.CreateDbContext();

            var tenantName = tenant.Name?.Trim();
            if (string.IsNullOrWhiteSpace(tenantName))
                throw new ArgumentException("Tenant name is required.", nameof(tenant));

            var tenantDbExists = await _appContext.TenantDatabase
                .AnyAsync(x => x.Id == tenant.TenantDBId);
            if (!tenantDbExists)
                throw new InvalidOperationException($"Tenant database with id '{tenant.TenantDBId}' does not exist.");

            var duplicateName = await _appContext.Tenants
                .AnyAsync(x => x.Name == tenantName);
            if (duplicateName)
                throw new InvalidOperationException($"A tenant with the name '{tenantName}' already exists.");

            TenantEntity t = new();
            t.Name = tenantName;
            t.TenantDBId = tenant.TenantDBId;
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
            _appContext.Tenants.Add(t);
            await _appContext.SaveChangesAsync();
            return t.Id;
        }
        public async Task<bool> UpdateAsync(TenantEntity tenant)
        {
            using var _appContext = ContextFactory.CreateDbContext();
            var t = await _appContext.Tenants.FirstOrDefaultAsync(x => x.Id == tenant.Id);
            if (t == null) { return false; }

            var tenantName = tenant.Name?.Trim();
            if (string.IsNullOrWhiteSpace(tenantName))
                throw new ArgumentException("Tenant name is required.", nameof(tenant));

            var tenantDbExists = await _appContext.TenantDatabase
                .AnyAsync(x => x.Id == tenant.TenantDBId);
            if (!tenantDbExists)
                throw new InvalidOperationException($"Tenant database with id '{tenant.TenantDBId}' does not exist.");

            var duplicateName = await _appContext.Tenants
                .AnyAsync(x => x.Id != tenant.Id && x.Name == tenantName);
            if (duplicateName)
                throw new InvalidOperationException($"A tenant with the name '{tenantName}' already exists.");

            t.Name = tenantName;
            t.TenantDBId = tenant.TenantDBId;
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
            _appContext.Tenants.Update(t);
            await _appContext.SaveChangesAsync();
            return true;
        }
        public async Task<IList<string>> GetRolesAsync(string username)
        {
            using var scope = _scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByNameAsync(username);
            if (user == null)
                return new List<string>();
            var roles = await userManager.GetRolesAsync(user);
            return roles;
        }

        public async Task<ShardingSingleDbContext> CreateDbContext(int tenantId)
        {
            using var _appContext = ContextFactory.CreateDbContext();
            string? ConnectionString = await _appContext.Tenants
                .Where(x => x.Id == tenantId)
                .Select(x => x.TenantDB.ConnectionString)
                .FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(ConnectionString))
                throw new Exception($"No database found for tenant {tenantId}");
            var optionsBuilder = new DbContextOptionsBuilder<ShardingSingleDbContext>();
            optionsBuilder.UseSqlServer(ConnectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });
            var db = new ShardingSingleDbContext(optionsBuilder.Options)
            {
                TenantId = tenantId
            };

            return db;
        }

        private async Task EnsureTenantDatabaseReadyAsync(int tenantId)
        {
            await using var dbtenant = await CreateDbContext(tenantId);

            var hasMigrations = dbtenant.Database.GetMigrations().Any();
            if (hasMigrations)
            {
                await dbtenant.Database.MigrateAsync();
                await EnsureLegacyUsersTableCompatibilityAsync(dbtenant);
                return;
            }

            var relationalCreator = dbtenant.Database.GetService<IRelationalDatabaseCreator>();
            var databaseExists = await relationalCreator.ExistsAsync();

            if (!databaseExists)
            {
                await dbtenant.Database.EnsureCreatedAsync();
                await EnsureLegacyUsersTableCompatibilityAsync(dbtenant);
                return;
            }

            var hasTables = await relationalCreator.HasTablesAsync();
            if (!hasTables)
            {
                await dbtenant.Database.EnsureCreatedAsync();
                await EnsureLegacyUsersTableCompatibilityAsync(dbtenant);
                return;
            }

            await EnsureLegacyUsersTableCompatibilityAsync(dbtenant);

            throw new InvalidOperationException(
                $"Tenant database for tenant '{tenantId}' exists but no EF migrations are configured for '{nameof(ShardingSingleDbContext)}'. " +
                "Create tenant migrations and run database update, or provision a fresh tenant database.");
        }

        private static async Task EnsureLegacyUsersTableCompatibilityAsync(ShardingSingleDbContext dbtenant)
        {
            const string sql = """
                IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NULL
                   AND OBJECT_ID(N'[dbo].[Users]', N'SN') IS NULL
                   AND OBJECT_ID(N'[dbo].[User]', N'U') IS NOT NULL
                BEGIN
                    EXEC(N'CREATE SYNONYM [dbo].[Users] FOR [dbo].[User]');
                END
                """;

            await dbtenant.Database.ExecuteSqlRawAsync(sql);
        }
        public async Task<bool> RemoveTenant(int TenantId)
        {
            var dataAccess = await CreateDbContext(TenantId);

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

            using var _appContext = ContextFactory.CreateDbContext();
            var users = await _appContext.Users.Where(x => x.TenantId == TenantId).ToListAsync();
            foreach (var u in users)
            {
                _appContext.Users.Remove(u);
            }
            await _appContext.SaveChangesAsync();
            var tenant = await _appContext.Tenants.FirstOrDefaultAsync(x => x.Id == TenantId);
            if (tenant != null)
            {
                _appContext.Tenants.Remove(tenant);
                await _appContext.SaveChangesAsync();
            }

            return false;
        }

        public async Task<bool> AddBasicCompanyInfoAsync(int tenantId)
        {
            var dataAccess = await CreateDbContext(tenantId);

            if (!await dataAccess.ResourceStatus.AnyAsync())
            {
                dataAccess.ResourceStatus.AddRange(
                    new StatusResourcesEntity("Active", "#16a34a", 10, true),
                    new StatusResourcesEntity("Inactive", "#dc2626", 20, true));
            }

            if (!await dataAccess.TaskStatus.AnyAsync())
            {
                dataAccess.TaskStatus.AddRange(
                    new TaskStatusEntity("Planned", "#2563eb", 10, true),
                    new TaskStatusEntity("In Progress", "#f59e0b", 20, true),
                    new TaskStatusEntity("Done", "#16a34a", 30, true));
            }

            if (!await dataAccess.Department.AnyAsync())
            {
                dataAccess.Department.AddRange(
                    DepartmentEntity.Create(new DepartmentBase { Name = "Management", Description = "Default management department" }),
                    DepartmentEntity.Create(new DepartmentBase { Name = "Engineering", Description = "Default engineering department" }));
            }

            if (!await dataAccess.ResourceTypes.AnyAsync())
            {
                var order = 10;
                foreach (var type in Enum.GetValues<ResourceTypesEnum>())
                {
                    dataAccess.ResourceTypes.Add(ResourceTypeEntity.Create(new PostResourceTypeDTO
                    {
                        Name = type.ToString(),
                        IsVisible = true,
                        Type = type,
                    }, order));
                    order += 10;
                }
            }

            if (!await dataAccess.Compensations.AnyAsync())
            {
                dataAccess.Compensations.Add(new CompensationEntity("Fixed price", "#8b5cf6", 10, true));
            }

            if (!await dataAccess.Contracts.AnyAsync())
            {
                dataAccess.Contracts.Add(new ContractEntity("Standard contract", "#0ea5e9", 10, true));
            }

            if (!await dataAccess.ProcurementMethod.AnyAsync())
            {
                var procurementMethod = new ProcurementMethodEntity();
                procurementMethod.Update("Direct purchase", "#f97316", 10, true);
                dataAccess.ProcurementMethod.Add(procurementMethod);
            }

            if (!await dataAccess.CalculationStatus.AnyAsync())
            {
                var status = new StatusEntity();
                status.Update("Open", "#22c55e", 10, true);
                dataAccess.CalculationStatus.Add(status);
            }

            if (!await dataAccess.CalcProjectType.AnyAsync())
            {
                var type = new TypeEntity();
                type.Update("General", "#3b82f6", 10, true);
                dataAccess.CalcProjectType.Add(type);
            }

            var changed = await dataAccess.SaveChangesAsync();
            return changed > 0;
        }

        public async Task<bool> UpdateUserAsync(UserPostDTO user, int? tentnid)
        {
            using var _appContext = ContextFactory.CreateDbContext();
            ApplicationUser? olduser = await _appContext.Users.FindAsync(user.Id);
            if (olduser == null || olduser.TenantId != tentnid) return false;

            olduser.Firstname = user.Firstname;
            olduser.Lastname = user.Lastname;
            olduser.PhoneNumber = user.PhoneNumber;
            olduser.PhoneNumberConfirmed = user.PhoneNumberConfirmed;
            olduser.LockoutEnabled = user.LockoutEnabled;
            olduser.LockoutStart = user.LockoutStart;
            olduser.LockoutEnd = user.LockoutEnd;

            try
            {
                if (tentnid.HasValue)
                {
                    var dataAccess = await CreateDbContext(tentnid.Value);
                    var tenantUser = await dataAccess.User.FirstOrDefaultAsync(x =>
                        (olduser.UserId.HasValue && x.Id == olduser.UserId.Value) ||
                        x.ExternalAuthId == olduser.Id);

                    if (tenantUser != null)
                    {
                        tenantUser.DepartmentId = user.DepartmentId;
                        tenantUser.FirstName = user.Firstname;
                        tenantUser.LastName = user.Lastname;
                        tenantUser.ExternalAuthId = olduser.Id;
                        await dataAccess.SaveChangesAsync();

                        if (!olduser.UserId.HasValue)
                        {
                            olduser.UserId = tenantUser.Id;
                        }
                    }
                }

                await _appContext.SaveChangesAsync();
            }
            catch (RetryLimitExceededException ex)
            {
                _logger.LogError(ex,
                    "Retry limit exceeded while updating user {UserId} for tenant {TenantId}.",
                    user.Id,
                    tentnid);
                return false;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Database update failed while updating user {UserId} for tenant {TenantId}.",
                    user.Id,
                    tentnid);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error while updating user {UserId} for tenant {TenantId}.",
                    user.Id,
                    tentnid);
                return false;
            }

            if (!await _userManager.IsInRoleAsync(olduser, user.Role))
            {
                var roles = await _userManager.GetRolesAsync(olduser);
                await _userManager.RemoveFromRolesAsync(olduser, roles);
                var addRoleResult = await _userManager.AddToRoleAsync(olduser, user.Role);
                if (!addRoleResult.Succeeded)
                {
                    return false;
                }
            }

            return true;
        }
        public async Task<bool> RemoveUserAsync(string id, int? tenantId)
        {
            using var _appContext = ContextFactory.CreateDbContext();
            bool result = false;
            var user = await _appContext.Users.FirstOrDefaultAsync(x => x.Id == id && (tenantId == null || x.TenantId == tenantId));
            if (user == null) return false;
            if (tenantId.HasValue && user.TenantId.HasValue)
            {
                var dataAccess = await CreateDbContext(user.TenantId.Value);
                var usert = dataAccess.User.Find(user.UserId);
                if (usert == null) return false;
                var calcs = dataAccess.Calculations.Where(x => x.IsPrivate && x.CreatedBy == user.UserId);
                if (calcs != null)
                    dataAccess.Calculations.RemoveRange(calcs);

                dataAccess.User.Remove(usert);
                await dataAccess.SaveChangesAsync();
            }
            if (result || !tenantId.HasValue)
            {
                _appContext.Users.Remove(user);
                await _appContext.SaveChangesAsync();
                return true;
            }
            return false;
        }
        public async Task<bool> BlockTenantAsync(int TenantId, bool block)
        {
            using var _appContext = ContextFactory.CreateDbContext();
            var users = await _appContext.Users.Where(x => x.TenantId == TenantId).ToListAsync();
            if (users == null || users.Count == 0) return false;
            foreach (var item in users)
            {
                item.LockoutEnabled = block;
            }
            await _appContext.SaveChangesAsync();
            return true;
        }
        public async Task<bool> BlockTenantAsync(string userid, bool block)
        {
            using var _appContext = ContextFactory.CreateDbContext();
            var user = await _appContext.Users.FirstOrDefaultAsync(x => x.Id == userid);
            if (user == null) return false;
            user.LockoutEnabled = block;
            await _appContext.SaveChangesAsync();
            return true;
        }
        public async Task<List<ApplicationUser>> GetUsersAsync(int? TenantId, int? department = null)
        {
            using var _appContext = ContextFactory.CreateDbContext();
            var users = _appContext.Users.AsQueryable();
            if (TenantId.HasValue && TenantId > 0)
            {
                users = users.Where(x => x.TenantId == TenantId && x.DepartmentId == department);
            }
            else users = users.Where(x => !x.TenantId.HasValue);
            return await users.ToListAsync();
        }
        public async Task<bool> RegisterAsync(UserPostDTO request, int? tenantId)
        {
            UserEntity? tenantUser = null;
            var userEntity = new ApplicationUser()
            {
                Email = request.Email,
                Firstname = request.Firstname,
                Lastname = request.Lastname,
                UserName = request.Email,
                TenantId = tenantId,
                DepartmentId = null,
                LockoutEnabled = request.LockoutEnabled,
                LockoutStart = request.LockoutStart,
                LockoutEnd = request.LockoutEnd,
                PhoneNumber = request.PhoneNumber,
                PhoneNumberConfirmed = request.PhoneNumberConfirmed,
            };

            try
            {
                if (tenantId.HasValue)
                {
                    await EnsureTenantDatabaseReadyAsync(tenantId.Value);

                    tenantUser = new UserEntity()
                    {
                        DepartmentId = request.DepartmentId,
                        FirstName = request.Firstname,
                        LastName = request.Lastname,
                        TenantId = tenantId.Value,
                        UserName = request.Email,
                        Email = request.Email,
                    };

                    await using var dbtenant = await CreateDbContext(tenantId.Value);
                    dbtenant.User.Add(tenantUser);
                    await dbtenant.SaveChangesAsync();

                    if (tenantUser.Id == 0)
                        return false;

                    userEntity.UserId = tenantUser.Id;
                }
            }
            catch (SqlException ex) when (ex.Number == 4060)
            {
                _logger.LogError(ex,
                    "Cannot open tenant database while creating tenant user for tenant {TenantId} and email {Email}. " +
                    "Verify the tenant connection string database exists and the SQL login has access.",
                    tenantId,
                    request.Email);
                return false;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while creating tenant user for tenant {TenantId} and email {Email}.", tenantId, request.Email);
                return false;
            }

            catch (RetryLimitExceededException ex)
            {
                _logger.LogError(ex, "Retry limit exceeded while creating tenant user for tenant {TenantId} and email {Email}.", tenantId, request.Email);
                return false;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failed while creating tenant user for tenant {TenantId} and email {Email}.", tenantId, request.Email);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating tenant user for tenant {TenantId} and email {Email}.", tenantId, request.Email);
                return false;
            }

            var result = await _userManager.CreateAsync(userEntity, request.Email);
            if (result.Succeeded)
            {
                if (tenantId.HasValue && tenantUser != null)
                {
                    try
                    {
                        tenantUser.ExternalAuthId = userEntity.Id;
                        await using var dbtenant = await CreateDbContext(tenantId.Value);
                        dbtenant.User.Update(tenantUser);
                        await dbtenant.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to link external auth id for tenant user {UserId} in tenant {TenantId}.", tenantUser.Id, tenantId);
                        await _userManager.DeleteAsync(userEntity);
                        return false;
                    }
                }

                await _userManager.GenerateEmailConfirmationTokenAsync(userEntity);
            }
            else if (tenantId.HasValue && tenantUser != null)
            {
                await using var dbtenant = await CreateDbContext(tenantId.Value);
                dbtenant.User.Remove(tenantUser);
                await dbtenant.SaveChangesAsync();
            }

            return result.Succeeded;
        }
    }
}
