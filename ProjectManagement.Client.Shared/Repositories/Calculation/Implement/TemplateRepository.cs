using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation.Implement
{
    public class TemplateRepository(HTTPRepository _httpRepository) : ITemplateRepository
    {
        static string TemplateURLBase => PMAPIConst.Template;
        public async Task<List<TemplateMVVM>> GetAsync(int? department)
        {
            if (department.HasValue)
                return await _httpRepository.GetAsync<List<TemplateMVVM>>(TemplateURLBase + URLConst.Template.GetByDepartment + $"/{department}");
            else return await _httpRepository.GetAsync<List<TemplateMVVM>>(TemplateURLBase + URLConst.GetAll);
        }
        public async Task<TemplateMVVM> GetByIdAsync(int id)
        {
            return await _httpRepository.GetAsync<TemplateMVVM>(TemplateURLBase + URLConst.Template.GetById + $"/{id}");
        }
        public async Task<TemplateMVVM> SetDefaultAsync(int id, int? newTemplate)
        {
            return await _httpRepository.GetAsync<TemplateMVVM>(TemplateURLBase + URLConst.Template.Set + $"/{id}/{newTemplate}");
           
        }
        public async Task<TemplateMVVM> CreateAsync(TemplateListPostDTO model)
        {
            return await _httpRepository.PostAsync<TemplateMVVM, TemplateListPostDTO>(model, TemplateURLBase);
        }
        public async Task<TemplateMVVM> CreateAsync(int? departmentId, TemplateListPostDTO model)
        {
            return await _httpRepository.PostAsync<TemplateMVVM, TemplateListPostDTO>(model, TemplateURLBase + URLConst.Template.PostAdmin + "/" + departmentId);
        }
        public async Task<bool> UpdateAsync(TemplateListPostDTO model, int id)
        {
            return await _httpRepository.PutAsync(model, TemplateURLBase + URLConst.Template.Update + $"/{id}");
        }
        public async Task<bool> DeleteAsync(int id)
        {
            return await _httpRepository.DeleteAsync(TemplateURLBase + id);
        }
    }
}
