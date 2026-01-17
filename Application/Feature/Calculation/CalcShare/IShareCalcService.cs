using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.CalcShare
{
    public interface IShareCalcService
    {
        Task<IReadOnlyList<ListShareCalcDTO>> GetAsync(int calculationId, int? departmentId, int userId, CancellationToken ct = default);
        Task<int> CreateAsync(PostShareCalcDTO dto, int fromUser, int fromDepartment, CancellationToken ct = default);
        Task<bool> UpdateAsync(UpdateShareCalcDTO dto, int fromUser, int fromDepartment, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, int departmentId, int userId, CancellationToken ct = default);


        Task<int> UpsertAsync(ShareCalcUpsertDTO dto, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    }
}
