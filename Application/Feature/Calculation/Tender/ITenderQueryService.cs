namespace Application.Services.CalculationItems.Tender
{
    using ProjectManagement.Shared.DTO.Calculation;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITenderQueryService
    {
        Task<TenderAttributeValuesListDTO> GetTenderListAsync(int calculationId, CancellationToken ct = default);

        Task<TenderDetailsDTO?> GetTenderDetailsAsync(
           int tenderId, int CalculationId,
            CancellationToken ct = default);
    }
}
