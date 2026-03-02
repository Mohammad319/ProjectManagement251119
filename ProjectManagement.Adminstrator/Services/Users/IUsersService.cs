using AuthPermissions.Entity;
using ProjectManagement.Shared.DTO.Tenant;
using ProjectManagement.Shared.Models.Account;

namespace ProjectManagement.Adminstrator.Services.Users
{
    public interface IUsersService
    {
        Task<bool> RemoveTenant(int TenantId);
        Task<IList<string>> GetRolesAsync(string username);
        Task<bool> RemoveUserAsync(string id, int? tenantId);
        Task<bool> BlockTenantAsync(int TenantId, bool block);
        Task<bool> BlockTenantAsync(string userid, bool block);
        Task<List<ApplicationUser>> GetUsersAsync(int? TenantId, int? department = null);
        Task<bool> RegisterAsync(UserPostDTO request, int? tenantId);
        Task<bool> UpdateUserAsync(UserPostDTO user, int? tentnid);

        Task<bool> UpdateAsync(TenantEntity tenant);
        Task<int> CreateAsync(TenantEntity tenant);
        Task<List<GetTenantsDTO>> GetAsync();
        Task<TenantEntity> GetByIdAsync(int id);
        Task<bool> AddBasicCompanyInfoAsync(int tenantId);

    }
}
