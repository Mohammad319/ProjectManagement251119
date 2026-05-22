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
            int? departmentId,
            CancellationToken ct = default);

        Task<bool> UpdateTenderAsync(
            int id,
            int calculationId,
            TenderPostDTO dto,
            int? departmentId,
            CancellationToken ct = default);

        Task<bool> DeleteTenderAsync(
            int id,
            int calculationId,
            int? departmentId,
            CancellationToken ct = default);

        Task<bool> UpdateTenderAttributeValueAsync(
            int tenderId,
            int attributeId,
            decimal value,
            int? departmentId,
            CancellationToken ct = default);
    }
}

