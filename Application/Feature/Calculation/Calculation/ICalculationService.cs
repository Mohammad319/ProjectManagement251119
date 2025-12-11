using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Feature.Calculation.Calculation
{
    public interface ICalculationService
    {
        Task<int> CreateAsync(CalculationPostDTO dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, CalculationPostDTO dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

        Task<int> CopyAsync(int id, Guid targetProjectId, CancellationToken ct = default);

        Task<bool> UpdateFactorsAsync(int id, List<OHFactors> model, CancellationToken ct = default);
        Task<bool> UpdateQuantityListAsync(int id, List<QuanityListDTO> model, CancellationToken ct = default);
        Task<bool> UpdateHourlyPriceListAsync(int id, List<HourlyPriceListGroupDTO> model, CancellationToken ct = default);

        Task<bool> ReorderAsync(int id, double newSortOrder, CancellationToken ct = default);
    }
}
