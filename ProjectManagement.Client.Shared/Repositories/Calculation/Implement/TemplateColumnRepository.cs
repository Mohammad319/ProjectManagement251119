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
    public class TemplateColumnRepository(HTTPRepository httpRepository) : ITemplateColumnRepository
    {
        private static string TemplateColumnUrlBase => PMAPIConst.TemplateColumn;

        public async Task<List<TemplateColumnMVVM>> GetAsync(int? department)
        {
            if (department.HasValue)
            {
                return (await httpRepository.GetAsync<List<TemplateColumnListDTO>>(TemplateColumnUrlBase + URLConst.TemplateColumn.GetByDepartment + $"/{department}"))
                    .Select(x => x.ToTemplateColumnMVVM())
                    .ToList();
            }

            return (await httpRepository.GetAsync<List<TemplateColumnListDTO>>(TemplateColumnUrlBase + URLConst.GetAll))
                .Select(x => x.ToTemplateColumnMVVM())
                .ToList();
        }

        public async Task<TemplateColumnMVVM> GetByIdAsync(int id)
        {
            var dto = await httpRepository.GetAsync<TemplateColumnModelDTO?>(TemplateColumnUrlBase + URLConst.TemplateColumn.GetById + $"/{id}");
            return dto?.ToTemplateColumnMVVM() ?? new TemplateColumnMVVM();
        }

        public async Task<TemplateColumnMVVM> SetDefaultAsync(int calcId, int? newTemplateColumnId)
        {
            var dto = await httpRepository.GetAsync<TemplateColumnModelDTO?>(TemplateColumnUrlBase + URLConst.TemplateColumn.Set + $"/{calcId}/{newTemplateColumnId}");
            return dto?.ToTemplateColumnMVVM() ?? new TemplateColumnMVVM();
        }

        public async Task<TemplateColumnMVVM> CreateAsync(TemplateColumnPostDTO model)
        {
            var dto = await httpRepository.PostAsync<TemplateColumnModelDTO, TemplateColumnPostDTO>(model, TemplateColumnUrlBase);
            return dto.ToTemplateColumnMVVM();
        }

        public async Task<TemplateColumnMVVM> CreateAsync(int? departmentId, TemplateColumnPostDTO model)
        {
            var dto = await httpRepository.PostAsync<TemplateColumnModelDTO, TemplateColumnPostDTO>(model, TemplateColumnUrlBase + URLConst.TemplateColumn.PostAdmin + "/" + departmentId);
            return dto.ToTemplateColumnMVVM();
        }

        public async Task<bool> UpdateAsync(TemplateColumnPostDTO model, int id)
        {
            return await httpRepository.PutAsync(model, TemplateColumnUrlBase + URLConst.TemplateColumn.Update + $"/{id}");
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await httpRepository.DeleteAsync(TemplateColumnUrlBase + id);
        }
    }
}
