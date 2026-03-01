using AuthPermissions.Context;
using AuthPermissions.Entity;
using Domain.Repository.AuthPermissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Models.Account;
namespace AuthPermissions.Services.Implement
{
    public class AuthRepository(ApplicationDbContext _appContext, UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole> _roleManager) : IAuthRepository
    {
        public static string[] GetRoles()
        {
            return [PMRolesConst.APP.Admin, PMRolesConst.APP.SuperManger, PMRolesConst.APP.Manger, PMRolesConst.APP.User,
            PMRolesConst.Tenant.Admin,PMRolesConst.Tenant.SuperManger,PMRolesConst.Tenant.Manger,PMRolesConst.Tenant.User];
        }
        public async Task<bool> Initialize(string email, string pass)
        {
            foreach (var role in GetRoles())
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole { Name = role, NormalizedName = role, ConcurrencyStamp = role });
            }

            var admin = await _userManager.FindByEmailAsync(email);
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                };

                var createResult = await _userManager.CreateAsync(admin, pass);
                if (!createResult.Succeeded)
                    return false;
            }

            if (!await _userManager.IsInRoleAsync(admin, PMRolesConst.APP.Admin))
            {
                var roleResult = await _userManager.AddToRoleAsync(admin, PMRolesConst.APP.Admin);
                if (!roleResult.Succeeded)
                    return false;
            }

            await SeedDefaultTenantsAsync();
            return true;
        }

        private async Task SeedDefaultTenantsAsync()
        {
            var tenantDatabases = new[]
            {
                new TenantDatabaseEntity
                {
                    Name = "DB1",
                    ConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=db922357028_2;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False;MultipleActiveResultSets=True"
                },
                new TenantDatabaseEntity
                {
                    Name = "DB2",
                    ConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=db962510648_2;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False;MultipleActiveResultSets=True"
                }
            };

            foreach (var tenantDatabase in tenantDatabases)
            {
                var exists = await _appContext.TenantDatabase
                    .AnyAsync(x => x.Name == tenantDatabase.Name || x.ConnectionString == tenantDatabase.ConnectionString);

                if (!exists)
                    _appContext.TenantDatabase.Add(tenantDatabase);
            }

            await _appContext.SaveChangesAsync();

            var persistedDatabases = await _appContext.TenantDatabase
                .Where(x => x.Name == "DB1" || x.Name == "DB2")
                .ToDictionaryAsync(x => x.Name, x => x.Id);

            var tenants = new[]
            {
                new TenantEntity { Name = "Company 1", TenantDBId = persistedDatabases.GetValueOrDefault("DB1") },
                new TenantEntity { Name = "Company 2", TenantDBId = persistedDatabases.GetValueOrDefault("DB2") }
            };

            foreach (var tenant in tenants)
            {
                if (tenant.TenantDBId is null or <= 0)
                    continue;

                var exists = await _appContext.Tenants.AnyAsync(x => x.Name == tenant.Name);
                if (!exists)
                    _appContext.Tenants.Add(tenant);
            }

            await _appContext.SaveChangesAsync();
        }
        public async Task<IEnumerable<UserAuthModel>> GetUsersAsync(int? TenantId, int? DepartmentId)
        {
            var users = _appContext.Users.AsQueryable();
            if (TenantId.HasValue && TenantId > 0)
            {
                users = users.Where(x => x.TenantId == TenantId);
                if (DepartmentId.HasValue && DepartmentId > 0)
                    users = users.Where(x => x.DepartmentId == DepartmentId);
                else users = users.Where(x => x.DepartmentId == null);
            }
            else users = users.Where(x => !x.TenantId.HasValue);
            IList<UserAuthModel> model = [];
            foreach (var user in await users.ToListAsync())
            {
                model.Add(new UserAuthModel()
                {
                    Firstname = user.Firstname ?? string.Empty,
                    Lastname = user.Lastname ?? string.Empty,
                    LockoutStart = user.LockoutStart,
                    Email = user.Email ?? string.Empty,
                    UserId = user.UserId,
                    DepartmentId = user.DepartmentId,
                    Username = user.UserName ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    Id = user.Id,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    NormalizedEmail = user.NormalizedEmail ?? string.Empty,
                    //Roles = await _userManager.GetRolesAsync(user)
                });
            }

            return model;
        }
    }
}
