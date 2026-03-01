using AuthPermissions.Context;
using AuthPermissions.Entity;
using Domain.Repository.AuthPermissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Models.Account;
namespace AuthPermissions.Services.Implement
{
    public class AuthRepository(ApplicationDbContext _appContext, UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole> _roleManager,
        IConfiguration _configuration) : IAuthRepository
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
            // IMPORTANT:
            // - لا تضع connection strings داخل الكود.
            // - هذا seed مخصص للـ DEV فقط ويقرأ من appsettings.Development.json (أو UserSecrets/ENV).
            var dbSeeds = _configuration
                .GetSection("TenantSeeds:Databases")
                .Get<List<TenantDatabaseSeed>>() ?? [];

            var tenantSeeds = _configuration
                .GetSection("TenantSeeds:Tenants")
                .Get<List<TenantSeed>>() ?? [];

            if (dbSeeds.Count == 0 || tenantSeeds.Count == 0)
                return;

            // 1) Upsert TenantDatabases
            foreach (var seed in dbSeeds)
            {
                if (string.IsNullOrWhiteSpace(seed.Name) || string.IsNullOrWhiteSpace(seed.ConnectionString))
                    continue;

                var name = seed.Name.Trim();
                var cs = seed.ConnectionString.Trim();

                var exists = await _appContext.TenantDatabase
                    .AnyAsync(x => x.Name == name || x.ConnectionString == cs);

                if (!exists)
                {
                    _appContext.TenantDatabase.Add(new TenantDatabaseEntity
                    {
                        Name = name,
                        ConnectionString = cs
                    });
                }
            }

            await _appContext.SaveChangesAsync();

            // 2) Resolve DB ids
            var dbMap = await _appContext.TenantDatabase
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Name, x => x.Id);

            // 3) Upsert Tenants
            foreach (var seed in tenantSeeds)
            {
                if (string.IsNullOrWhiteSpace(seed.Name) || string.IsNullOrWhiteSpace(seed.Database))
                    continue;

                var name = seed.Name.Trim();
                var dbName = seed.Database.Trim();

                if (!dbMap.TryGetValue(dbName, out var dbId) || dbId <= 0)
                    continue;

                var exists = await _appContext.Tenants.AnyAsync(x => x.Name == name);
                if (!exists)
                {
                    _appContext.Tenants.Add(new TenantEntity
                    {
                        Name = name,
                        TenantDBId = dbId
                    });
                }
            }

            await _appContext.SaveChangesAsync();
        }

        private sealed class TenantDatabaseSeed
        {
            public string Name { get; set; } = string.Empty;
            public string ConnectionString { get; set; } = string.Empty;
        }

        private sealed class TenantSeed
        {
            public string Name { get; set; } = string.Empty;
            public string Database { get; set; } = string.Empty;
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
