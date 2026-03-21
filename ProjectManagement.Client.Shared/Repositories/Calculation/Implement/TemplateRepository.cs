using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Mapping;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation.Implement
{
    public class TemplateRepository(HTTPRepository _httpRepository) : ITemplateRepository
    {
        static string TemplateURLBase => PMAPIConst.Template;
        public async Task<List<TemplateMVVM>> GetAsync(int? department)
        {
            if (department.HasValue)
            {
                return (await _httpRepository.GetAsync<List<TemplateListDTO>>(TemplateURLBase + URLConst.Template.GetByDepartment + $"/{department}"))
                    .Select(x => x.ToTemplateMVVM())
                    .ToList();
            }

            return (await _httpRepository.GetAsync<List<TemplateListDTO>>(TemplateURLBase + URLConst.GetAll))
                .Select(x => x.ToTemplateMVVM())
                .ToList();
        }
        public async Task<TemplateMVVM> GetByIdAsync(int id)
        {
            var dto = await _httpRepository.GetAsync<TemplateModelDTO>(TemplateURLBase + URLConst.Template.GetById + $"/{id}");
            return dto.ToTemplateMVVM();
        }
        public async Task<TemplateMVVM> SetDefaultAsync(int id, int? newTemplate)
        {
            var dto = await _httpRepository.GetAsync<TemplateModelDTO>(TemplateURLBase + URLConst.Template.Set + $"/{id}/{newTemplate}");
            return dto.ToTemplateMVVM();
           
        }
        public async Task<TemplateMVVM> CreateAsync(TemplateListPostDTO model)
        {
            var dto = await _httpRepository.PostAsync<TemplateModelDTO, TemplateListPostDTO>(model, TemplateURLBase);
            return dto.ToTemplateMVVM();
        }
        public async Task<TemplateMVVM> CreateAsync(int? departmentId, TemplateListPostDTO model)
        {
            var dto = await _httpRepository.PostAsync<TemplateModelDTO, TemplateListPostDTO>(model, TemplateURLBase + URLConst.Template.PostAdmin + "/" + departmentId);
            return dto.ToTemplateMVVM();
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
