using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Feature.Calculation.Calculation
{
    public interface ICalculationQueryService
    {
        Task<IReadOnlyList<ListCalculationDTO>> GetAllAsync(
            Guid projectId,
            bool isArchived,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default,
            bool isViewer = false);

        Task<IEnumerable<ListCalculationDTO>> GetByDepartmentAsync(
            Guid projectId,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default,
            bool isViewer = false);

        Task<CalculationDetailsDTO?> GetDetailsAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default,
            bool isViewer = false);

        Task<CalculationPostDTO?> GetPostModelAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default,
            bool isViewer = false);

        Task<CalculationPageDTO?> GetPageAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default,
            bool isViewer = false);

        Task<List<HourlyPriceListGroupDTO>> GetHourlyPriceListAsync(
            int id,
            int? departmentId,
            CancellationToken cancellationToken = default);
    }
}
