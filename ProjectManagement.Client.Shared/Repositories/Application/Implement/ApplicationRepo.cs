using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Mapping;
using ProjectManagement.Client.Shared.Model.Application;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.App;

namespace ProjectManagement.Client.Shared.Repositories.Application.Implement
{
    public class ApplicationRepo(HTTPRepository httpRepository) : IApplicationRepo
    {
        static string ApplicationURLBase => PMAPIConst.Application;

        public async Task<List<ApplicationModel>> GetApplicationsAsync(bool withNoneVisible)
        {
            var dtos = await httpRepository.GetAsync<List<ApplicationDTO>>(ApplicationURLBase + $"?wnv={withNoneVisible}");
            return dtos?.Select(x => x.ToApplicationModel()).ToList() ?? [];
        }

        public async Task<List<ApplicationValuesModel>> GetCalcAppValuesAsync(int calcId)
        {
            var dtos = await httpRepository.GetAsync<List<ApplicationValuesDTO>>(ApplicationURLBase + URLConst.Application.AppCalculationValues + $"/{calcId}");
            return dtos?.Select(x => x.ToApplicationValuesModel()).ToList() ?? [];
        }

        public async Task<int> CreateAsync(ApplicationValuesModel model)
        {
            return await httpRepository.PostAsync<int, ApplicationValuesDTO>(model.ToApplicationValuesDto(), ApplicationURLBase + URLConst.Application.AppCalculationValues);
        }

        public async Task<bool> UpdateAsync(ApplicationValuesModel model)
        {
            return await httpRepository.PutAsync(model.ToApplicationValuesDto(), ApplicationURLBase + URLConst.Application.AppCalculationValues);
        }
    }
}
