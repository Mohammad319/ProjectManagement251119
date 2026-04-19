using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation
{
    public interface ITemplateColumnRepository
    {
        Task<List<TemplateColumnMVVM>> GetAsync(int? department = null);
        Task<TemplateColumnMVVM> GetByIdAsync(int id);
        Task<TemplateColumnMVVM> SetDefaultAsync(int calcId, int? newTemplateColumnId);
        Task<TemplateColumnMVVM> CreateAsync(TemplateColumnPostDTO templateColumn);
        Task<TemplateColumnMVVM> CreateAsync(int? departmentId, TemplateColumnPostDTO templateColumn);
        Task<bool> UpdateAsync(TemplateColumnPostDTO templateColumn, int id);
        Task<bool> DeleteAsync(int id);
    }
}
