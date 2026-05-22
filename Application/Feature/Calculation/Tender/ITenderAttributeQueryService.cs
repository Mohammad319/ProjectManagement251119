namespace Application.Services.CalculationItems.Tender
{
    using ProjectManagement.Shared.DTO.Calculation;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITenderAttributeQueryService
    {
        Task<List<TenderAttributeListDTO>> GetAttributesAsync(int calculationId, int? departmentId, CancellationToken ct = default);
    }
}
