using ProjectManagement.Shared.Models.Account;

namespace Domain.Repository.AuthPermissions
{
    public interface IAuthRepository
    {
        Task<IEnumerable<UserAuthModel>> GetUsersAsync(int? tenantId, int? departmentId);
        Task<bool> Initialize(string email, string pass);
    }
}
