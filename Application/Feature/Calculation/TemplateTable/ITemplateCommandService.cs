using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Services.CalculationItems.TemplateTable
{
    public interface ITemplateCommandService
    {
        Task<TemplateModelDTO> CreateAsync(TemplateListPostDTO dto, int? departmentId, CancellationToken ct);
        Task<bool> UpdateAsync(int id, TemplateListPostDTO dto, int? departmentId, CancellationToken ct);
        Task<bool> DeleteAsync(int id, int? departmentId, CancellationToken ct);
        Task<TemplateModelDTO?> CopyAsync(int id, int? sourceDepartmentId, int? targetDepartmentId, CancellationToken ct);
        Task<TemplateModelDTO?> SetDefaultAsync(int calculationId, int? templateId, int? departmentId, CancellationToken ct);

        /// <summary>Marks the appearance template as the standardval for its scope (company or department),
        /// clearing the flag on the other templates in that same scope.</summary>
        Task<bool> SetScopeDefaultAsync(int id, int? departmentId, CancellationToken ct);
    }

    public interface ITemplateColumnCommandService
    {
        Task<TemplateColumnModelDTO> CreateAsync(TemplateColumnPostDTO dto, int? departmentId, CancellationToken ct);
        Task<bool> UpdateAsync(int id, TemplateColumnPostDTO dto, int? departmentId, CancellationToken ct);
        Task<bool> DeleteAsync(int id, int? departmentId, CancellationToken ct);
        Task<TemplateColumnModelDTO?> CopyAsync(int id, int? sourceDepartmentId, int? targetDepartmentId, CancellationToken ct);
        Task<TemplateColumnModelDTO?> SetDefaultAsync(int calculationId, int? templateColumnId, int? departmentId, CancellationToken ct);

        /// <summary>Marks the column template as the standardval for its scope (company or department),
        /// clearing the flag on the other templates in that same scope.</summary>
        Task<bool> SetScopeDefaultAsync(int id, int? departmentId, CancellationToken ct);
    }
}
