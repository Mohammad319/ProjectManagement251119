using Domain.DTO.User;

namespace ProjectManagement.Services
{
    public interface ITenantContext
    {
        int TenantId { get; }
        int? UserId { get; }
        int? DepartmentId { get; }
    }

    public sealed class TenantContext : ITenantContext
    {
        public int TenantId { get; set; }
        public int? UserId { get; set; }
        public int? DepartmentId { get; set; }
    }

    public interface ITenantUserService
    {
        Task<bool> RecreateUserAsync(TenantUserDto tenantUser, CancellationToken ct = default);
        Task<List<TenantUserDto>> GetAllTenantUsersAsync(int? department, CancellationToken ct = default);
        Task<bool> UpdateUserAsync(TenantUserDto user, CancellationToken ct = default);
        Task<bool> RegisterAsync(TenantUserDto request, CancellationToken ct = default);
        Task<bool> RemoveAsync(string id, bool onlyfromregister, int userid, CancellationToken ct = default);
    }

}
