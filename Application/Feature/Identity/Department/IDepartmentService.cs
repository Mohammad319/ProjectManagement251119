using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Identity;

namespace Application.Feature.Identity.Department
{
    public interface IDepartmentService
    {
        // Commands
        Task<int> CreateAsync(DepartmentBase dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, DepartmentBase dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

        // Queries
        Task<List<ListDTO>> GetAsListAsync(CancellationToken ct = default);
        Task<List<DepartmentDetailsDTO>> GetDetailsAsync(CancellationToken ct = default);
    }
}
