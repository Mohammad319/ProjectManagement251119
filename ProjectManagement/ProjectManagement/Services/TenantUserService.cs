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

    /// <summary>Aggregated outcome of a bulk user action. <see cref="SkippedProtected"/> counts users
    /// left untouched because the action would have stripped the tenant of its last administrator.</summary>
    public readonly record struct BulkUserActionResult(int Succeeded, int Failed, int SkippedProtected)
    {
        public int Total => Succeeded + Failed + SkippedProtected;
    }

    public interface ITenantUserService
    {
        Task<bool> RecreateUserAsync(TenantUserDto tenantUser, CancellationToken ct = default);
        Task<List<TenantUserDto>> GetAllTenantUsersAsync(int? department, CancellationToken ct = default);
        Task<bool> UpdateUserAsync(TenantUserDto user, CancellationToken ct = default);
        Task<bool> RegisterAsync(TenantUserDto request, CancellationToken ct = default);
        Task<bool> RemoveAsync(string id, bool onlyfromregister, int userid, CancellationToken ct = default);

        /// <summary>Locks or unlocks an auth user's sign-in. Locking signs them out and blocks future logins.</summary>
        Task<bool> SetLockoutAsync(string authId, bool locked, CancellationToken ct = default);

        /// <summary>Activates or deactivates an auth user. A deactivated account is signed out and cannot sign in
        /// (a permanent, offboarding-style state distinct from the temporary lockout).</summary>
        Task<bool> SetActiveAsync(string authId, bool active, CancellationToken ct = default);

        /// <summary>Reassigns a set of local users (and their linked auth users) to a department. Pass null to clear.</summary>
        Task<int> SetDepartmentAsync(IReadOnlyCollection<int> userIds, int? departmentId, CancellationToken ct = default);

        /// <summary>Changes the tenant role of a single auth user. Switching to Admin clears the department.</summary>
        Task<bool> SetRoleAsync(string authId, string role, CancellationToken ct = default);

        /// <summary>Number of tenant administrators in the current tenant.</summary>
        Task<int> CountTenantAdminsAsync(CancellationToken ct = default);

        /// <summary>Generates a new temporary password for an auth user and emails it to them.</summary>
        Task<bool> ResetPasswordAsync(string authId, CancellationToken ct = default);

        /// <summary>Locks/unlocks many users in one call, always keeping at least one active administrator.</summary>
        Task<BulkUserActionResult> SetLockoutBulkAsync(IReadOnlyCollection<string> authIds, bool locked, CancellationToken ct = default);

        /// <summary>Changes the role of many users in one call, never demoting the last administrator.</summary>
        Task<BulkUserActionResult> SetRoleBulkAsync(IReadOnlyCollection<string> authIds, string role, CancellationToken ct = default);

        /// <summary>Deletes many users in one call, never removing the last administrator.</summary>
        Task<BulkUserActionResult> RemoveBulkAsync(IReadOnlyCollection<(string? AuthId, int UserId)> users, CancellationToken ct = default);
    }

}
