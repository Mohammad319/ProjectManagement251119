using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Feature.Calculation.Calculation
{
    public interface ICalculationQueryService
    {
        Task<IReadOnlyList<ListCalculationDTO>> GetAllAsync(
            Guid projectId,
            bool isVisible,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<ListCalculationDTO>> GetByDepartmentAsync(
            Guid projectId,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<CalculationDetailsDTO?> GetDetailsAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<CalculationPostDTO?> GetPostModelAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<CalculationPageDTO?> GetPageAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<List<HourlyPriceListGroupDTO>> GetHourlyPriceListAsync(
            int id,
            int? departmentId,
            CancellationToken cancellationToken = default);
    }
}
