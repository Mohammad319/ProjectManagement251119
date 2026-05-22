namespace Application.Services.CalculationItems.Tender
{
    using ProjectManagement.Shared.DTO.Calculation;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITenderAttributeCommandService
    {
        Task<int> CreateAttributeAsync(
            TenderAttributeListPostDTO dto,
            int calculationId,
            int? departmentId,
            CancellationToken ct = default);

        Task<bool> UpdateAttributeAsync(
            int id,
            int calculationId,
            TenderAttributePostDTO dto,
            int? departmentId,
            CancellationToken ct = default);

        Task<bool> DeleteAttributeAsync(
            int id,
            int calculationId,
            int? departmentId,
            CancellationToken ct = default);
    }
}
