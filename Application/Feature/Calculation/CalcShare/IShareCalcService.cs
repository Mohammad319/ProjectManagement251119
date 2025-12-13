using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Services.CalculationItems.CalcShare
{
    public interface IShareCalcService
    {
        Task<IEnumerable<ListShareCalcDTO>> GetAsync(int calculationId, int? departmentId, int userId, CancellationToken ct = default);
        Task<int> CreateAsync(PostShareCalcDTO dto, int fromUser, int fromDepartment, CancellationToken ct = default);
        Task<bool> UpdateAsync(UpdateShareCalcDTO dto, int fromUser, int fromDepartment, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, int departmentId, int userId, CancellationToken ct = default);


        Task<int> UpsertAsync(ShareCalcUpsertDTO dto, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    }
}
