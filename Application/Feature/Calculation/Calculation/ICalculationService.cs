using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System;

namespace Application.Feature.Calculation.Calculation
{
    public interface ICalculationService
    {
        Task<int> CreateAsync(
            CalculationPostDTO dto,
            Guid projectId,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(
            int id,
            CalculationPostDTO dto,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<int> CopyAsync(
            int id,
            Guid projectId,
            int? departmentId,
            int userId,
            bool allowCrossDepartment,
            CancellationToken cancellationToken = default);

        Task<bool> MoveAsync(
            int id,
            Guid projectId,
            int? departmentId,
            int userId,
            bool allowCrossDepartment,
            CancellationToken cancellationToken = default);

        Task<bool> NewOrderAsync(
            int id,
            int newOrder,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateHourlyPriceListAsync(
            int id,
            List<HourlyPriceListGroupDTO> hourlyPriceList,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateFactorsAsync(
            int id,
            List<OHFactors> factors,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateQuantityListAsync(
            int id,
            List<QuanityListDTO> model,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateSortAsync(
            int id,
            SortConfig sort,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateDisplayPresetsAsync(
            int id,
            DisplayOptionsPresetStore store,
            int? departmentId,
            CancellationToken cancellationToken = default);
    }
}
