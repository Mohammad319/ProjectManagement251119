using AuthPermissions.Entity;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using Domain.Entities.ResourceType;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.Identity;
using ProjectManagement.Shared.DTO.ResourceType;
using ProjectManagement.Shared.DTO.Tenant;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Models.Account;

namespace ProjectManagement.Adminstrator.Services.Users
{
    public class UsersService(UserManager<ApplicationUser> _userManager,
        IServiceScopeFactory _scopeFactory,
          IDbContextFactory<ApplicationDbContext> ContextFactory
       ) : IUsersService
    {
        public async Task<bool> AddBasicCompanyInfoAsync(int tenantId, int? userId = null)
        {
            var dataAccess = await CreateDbContext(tenantId, userId);
            var changed = 0;

            var defaultResourceStatuses = new (string Name, string Color, int SortOrder, bool IsVisible)[]
            {
                ("Active", "#16a34a", 10, true),
                ("Inactive", "#dc2626", 20, true),
            };

            var existingResourceStatuses = await dataAccess.ResourceStatus.ToListAsync();
            foreach (var defaultResourceStatus in defaultResourceStatuses)
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

            var defaultTaskStatuses = new (string Name, string Color, int SortOrder, bool IsVisible)[]
            {
                ("Planned", "#2563eb", 10, true),
                ("In Progress", "#f59e0b", 20, true),
                ("Done", "#16a34a", 30, true),
            };

            var existingTaskStatuses = await dataAccess.TaskStatus.ToListAsync();
            foreach (var defaultTaskStatus in defaultTaskStatuses)
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

            var defaultAccountGroups = new[] { "AG1", "AG2" };
            var existingAccountGroups = await dataAccess.AccountGroup.ToListAsync();
            foreach (var groupName in defaultAccountGroups)
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

            var defaultAccounts = new (string Code, string Name, string GroupName, bool IsVisible)[]
            {
                ("Code1", "Acc1", "AG1", true),
                ("Code2", "Acc2", "AG1", true),
                ("Code3", "Acc3", "AG2", true),
                ("Code4", "Acc4", "AG2", true),
            };

            var existingAccounts = await dataAccess.Accounts.ToListAsync();
            foreach (var defaultAccount in defaultAccounts)
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
            var order = 10;
            foreach (var type in Enum.GetValues<ResourceTypesEnum>())
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
                    order += 10;
                    continue;
                }

                resourceType.Update(resourceTypeDto);
                resourceType.UpdateOrder(order);
                order += 10;
            }

            if (!await dataAccess.Compensations.AnyAsync())
            {
                dataAccess.Compensations.Add(new CompensationEntity("Fixed price", "#8b5cf6", 10, true));
            }

            var defaultContracts = new (string Name, string Color, int SortOrder, bool IsVisible)[]
            {
                ("Standard contract", "#0ea5e9", 10, true),
            };

            var existingContracts = await dataAccess.Contracts.ToListAsync();
            foreach (var defaultContract in defaultContracts)
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

            var defaultProcurementMethods = new (string Name, string Color, int SortOrder, bool IsVisible)[]
            {
                ("Limited Procedure", "#00ff00", 1300, true),
                ("Selective Tending", "#00ff00", 1400, true),
                ("Open Tendering", "#00ff00", 1500, true),
            };

            var existingProcurementMethods = await dataAccess.ProcurementMethod.ToListAsync();
            foreach (var defaultProcurementMethod in defaultProcurementMethods)
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

            if (dataAccess.ChangeTracker.HasChanges())
            {
                changed += await dataAccess.SaveChangesAsync();
            }

            return changed > 0;
        }

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
            return await _appContext.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task<int> CreateAsync(TenantEntity tenant)
        {
            using var _appContext = ContextFactory.CreateDbContext();
            TenantEntity t = new();
            t.Name = tenant.Name;
            t.Street = tenant.Street;
            t.City = tenant.City;
            t.Country = tenant.Country;
            t.Fax = tenant.Fax;
            t.BuildNumber = tenant.BuildNumber;
            t.PostCode = tenant.PostCode;
            t.MaxCalculations = tenant.MaxCalculations;
            t.MaxUsers = tenant.MaxUsers;
            t.Fax = tenant?.Fax;
            t.Website = tenant?.Website;
            t.Phone = tenant?.Phone;
            t.Mobile = tenant?.Mobile;
            t.Email = tenant?.Email;
            t.DateExpire = tenant.DateExpire;
            t.Note = tenant?.Note;
            _appContext.Tenants.Add(t);
            await _appContext.SaveChangesAsync();
            return t.Id;
        }
        public async Task<bool> UpdateAsync(TenantEntity tenant)
        {
            using var _appContext = ContextFactory.CreateDbContext();
            var t = await _appContext.Tenants.FirstOrDefaultAsync(x => x.Id == tenant.Id);
            t.Name = tenant.Name;
            t.Street = tenant.Street;
            t.City = tenant.City;
            t.Country = tenant.Country;
            t.Fax = tenant.Fax;
            t.BuildNumber = tenant.BuildNumber;
            t.PostCode = tenant.PostCode;
            t.MaxCalculations = tenant.MaxCalculations;
            t.MaxUsers = tenant.MaxUsers;
            t.Fax = tenant?.Fax;
            t.Website = tenant?.Website;
            t.Phone = tenant?.Phone;
            t.Mobile = tenant?.Mobile;
            t.Email = tenant?.Email;
            t.DateExpire = tenant.DateExpire;

            t.Note = tenant?.Note;
            if (t == null) { return false; }
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
         async Task<ShardingSingleDbContext> CreateDbContext(int tenantId, int? currentUserId = null)
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
                TenantId = tenantId,
                CurrentUserId = currentUserId
            };

            return db;
        }

        public async Task<ShardingSingleDbContext> CreateDbContext(int tenantId)
        {
            using var _appContext = ContextFactory.CreateDbContext();
            string? ConnectionString = await _appContext?.Tenants?.Where(x => x.Id == tenantId)?
                .Select(x => x.TenantDB.ConnectionString)?.FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(ConnectionString))
                throw new Exception($"No database found for tenant {tenantId}");
            var optionsBuilder = new DbContextOptionsBuilder<ShardingSingleDbContext>();
            optionsBuilder.UseSqlServer(ConnectionString);
            var db = new ShardingSingleDbContext(optionsBuilder.Options)
            {
                TenantId = tenantId
            };

            return db;
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
            _appContext.Tenants.Remove(tenant);
            await _appContext.SaveChangesAsync();

            return false;
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
            if (tentnid.HasValue)
            {
                UserEntity userEntity = new()
                {
                    // = olduser.UserId.Value,
                    DepartmentId = user.DepartmentId,
                    FirstName = user.Firstname,
                    LastName = user.Lastname,
                    TenantId = tentnid.Value,
                    //Username = user.Email,
                    //Email = user.Email,
                    ExternalAuthId = user.Id
                };
                var dataAccess = await CreateDbContext(tentnid.Value);
                dataAccess.User.Update(userEntity);
                await dataAccess.SaveChangesAsync();
            }
            if (!await _userManager.IsInRoleAsync(olduser, user.Role))
            {
                var roles = await _userManager.GetRolesAsync(olduser);
                await _userManager.RemoveFromRolesAsync(olduser, roles);
                await _userManager.AddToRoleAsync(olduser, user.Role);
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
            int? userid = null;
            UserEntity? ue = null;
            if (tenantId.HasValue)
            {
                ue = new UserEntity()
                {
                    DepartmentId = request.DepartmentId,
                    FirstName = request.Firstname,
                    LastName = request.Lastname,
                    TenantId = tenantId.Value,
                    UserName = request.Email,
                    Email = request.Email,
                };
                var dbtenant = await CreateDbContext(tenantId.Value);
                dbtenant.User.Add(ue);
                await dbtenant.SaveChangesAsync();
            }
            var response = new RegisterDto.Response();
            var userEntity = new ApplicationUser()
            {
                Email = request.Email,
                Firstname = request.Firstname,
                Lastname = request.Lastname,
                UserName = request.Email,
                TenantId = tenantId,
                DepartmentId = null,
                UserId = userid,
                LockoutEnabled = request.LockoutEnabled,
                LockoutStart = request.LockoutStart,
                LockoutEnd = request.LockoutEnd,
                PhoneNumber = request.PhoneNumber,
                PhoneNumberConfirmed = request.PhoneNumberConfirmed,
            };
            if (tenantId.HasValue && ue != null && ue.Id == 0)
            {
                return false;
            }
            var result = await _userManager.CreateAsync(userEntity, request.Email);
            if (result.Succeeded)
            {
                if (tenantId.HasValue && ue != null)
                {
                    ue.ExternalAuthId = userEntity.Id;
                    var dbtenant = await CreateDbContext(tenantId.Value);
                    dbtenant.User.Update(ue);
                    await dbtenant.SaveChangesAsync();
                }
                await _userManager.AddToRoleAsync(userEntity, request.Role);
                response.IsSuccessfulRegistration = true;
                await _userManager.GenerateEmailConfirmationTokenAsync(userEntity);
            }
            return result.Succeeded;
        }
    }
}
