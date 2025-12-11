using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Feature.Calculation.Calculation
{
    public interface ICalculationQueryService
    {
        Task<IEnumerable<ListCalculationDTO>> GetAllAsync(Guid projectId, int userId, int? departmentId, CancellationToken ct);
        Task<IEnumerable<ListCalculationDTO>> GetByDepartmentAsync(Guid projectId, int userId, int? departmentId, CancellationToken ct);
        Task<CalculationDetailsDTO?> GetDetailsAsync(int id, CancellationToken ct);
        Task<CalculationPostDTO?> GetPostModelAsync(int id, CancellationToken ct);
        Task<CalculationPageDTO?> GetPageAsync(int id, int userId, int? departmentId, CancellationToken ct);
        Task<List<HourlyPriceListGroupDTO>> GetHourlyPriceListAsync(int id, int? departmentId, CancellationToken ct);
    }
}
