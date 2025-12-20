using AuthPermissions.Context;
using Domain.DTO.User;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.Constant;

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
        Task<bool> RecreateUserAsync(TenantUserDto tenantUser);
        Task<List<TenantUserDto>> GetAllTenantUsersAsync(int? department);
        Task<bool> UpdateUserAsync(TenantUserDto user);
        Task<bool> RegisterAsync(TenantUserDto request);
        Task<bool> RemoveAsync(string id, bool onlyfromregister, int userid);
    }

}
