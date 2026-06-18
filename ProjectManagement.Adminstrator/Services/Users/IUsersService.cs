using AuthPermissions.Entity;
using ProjectManagement.Shared.DTO.Tenant;
using ProjectManagement.Shared.Models.Account;

namespace ProjectManagement.Adminstrator.Services.Users
{
    public interface IUsersService
    {
        Task<CompanySeedResultDTO> AddBasicCompanyInfoAsync(int tenantId, int? userId = null);

        /// <summary>
        /// Seeds a sample folder with projects, calculations and a few tasks/resources so a fresh
        /// tenant has demo content to explore. Requires at least one tenant user and is idempotent
        /// (skips if the sample folder already exists).
        /// </summary>
        Task<CompanySeedResultDTO> AddSampleProjectDataAsync(int tenantId, int? userId = null);

        Task<bool> RemoveTenant(int TenantId);
        Task<IList<string>> GetRolesAsync(string username);

        /// <summary>Returns userId → single role name for every user in the scope, in one query (avoids N+1).</summary>
        Task<Dictionary<string, string?>> GetUserRolesMapAsync(int? tenantId);

        /// <summary>Resets a user to a freshly generated temporary password and e-mails it to them.</summary>
        Task<bool> ResetUserPasswordAsync(string userId, int? tenantId);

        Task<bool> RemoveUserAsync(string id, int? tenantId);
        Task<bool> BlockTenantAsync(int TenantId, bool block);
        Task<bool> BlockTenantAsync(string userid, bool block);

        /// <summary>Locks/unlocks a single user that belongs to the given tenant. Guards against
        /// self-lock and against locking the tenant's last admin.</summary>
        Task<bool> SetTenantUserLockoutAsync(string userId, int tenantId, bool block);
        Task<List<ApplicationUser>> GetUsersAsync(int? TenantId, int? department = null);
        Task<bool> RegisterAsync(UserPostDTO request, int? tenantId);
        Task<bool> UpdateUserAsync(UserPostDTO user, int? tentnid);

        Task<bool> UpdateAsync(TenantEntity tenant);
        Task<int> CreateAsync(TenantEntity tenant);
        Task<List<GetTenantsDTO>> GetAsync();
        Task<TenantEntity> GetByIdAsync(int id);
        Task<Dictionary<int, string>> GetDepartmentNamesAsync(int tenantId);

        /// <summary>Opens a short-timeout SQL connection to verify a tenant-database connection string.</summary>
        Task<(bool Ok, string? Error)> TestDatabaseConnectionAsync(string connectionString);

    }
}
