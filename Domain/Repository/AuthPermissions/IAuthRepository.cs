using ProjectManagement.Shared.DTO.Identity;
using ProjectManagement.Shared.Models.Account;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Domain.Repository.AuthPermissions
{
    public interface IAuthRepository
    {
        Task<IEnumerable<UserAuthModel>> GetUsersAsync(int? TenantId, int? DepartmentId);
        Task<bool> Initialize(string email, string pass);
    }
}
