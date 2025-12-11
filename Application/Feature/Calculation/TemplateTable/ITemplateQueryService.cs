namespace Application.Services.CalculationItems.TemplateTable
{
    using ProjectManagement.Shared.DTO.Calculation.Template;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITemplateQueryService
    {
        Task<TemplateModelDTO?> GetByIdAsync(
            int id,
            int? departmentId,
            CancellationToken ct = default);

        Task<List<TemplateListDTO>> GetByUserAsync(
            int? departmentId,
            CancellationToken ct = default);
    }
}
