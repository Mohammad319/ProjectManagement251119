namespace Application.Services.CalculationItems.Tender
{
    using ProjectManagement.Shared.DTO.Calculation;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITenderCommandService
    {
        Task<int> CreateTenderAsync(
            TenderPostDTO dto,
            int calculationId,
            int companyId,
            CancellationToken ct = default);

        Task<bool> UpdateTenderAsync(
            int id,
            int calculationId,
            TenderPostDTO dto,
            CancellationToken ct = default);

        Task<bool> DeleteTenderAsync(
            int id,
            int calculationId,
            CancellationToken ct = default);

        Task<bool> UpdateTenderAttributeValueAsync(
            int tenderId,
            int attributeId,
            decimal value,
            CancellationToken ct = default);
    }
}

