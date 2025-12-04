using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation
{
    public interface ITemplateRepository
    {
        Task<List<TemplateMVVM>> GetAsync(int? department = null);
        Task<TemplateMVVM> GetByIdAsync(int id);
        Task<TemplateMVVM> SetDefaultAsync(int calcID, int? newTmplateId);
        Task<TemplateMVVM> CreateAsync(TemplateListPostDTO template);
        Task<TemplateMVVM> CreateAsync(int? departmentId, TemplateListPostDTO template);
        Task<bool> UpdateAsync(TemplateListPostDTO temp, int id);
        Task<bool> DeleteAsync(int id);
    }
}
