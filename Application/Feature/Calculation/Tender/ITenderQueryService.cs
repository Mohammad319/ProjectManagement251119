namespace Application.Services.CalculationItems.Tender
{
    using ProjectManagement.Shared.DTO.Calculation;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITenderQueryService
    {
        Task<List<TenderListDTO>> GetTenderListAsync(
            int calculationId,
            CancellationToken ct = default);

        Task<TenderDetailsDTO?> GetTenderDetailsAsync(
           int tenderId, int CalculationId,
            CancellationToken ct = default);
    }
}
