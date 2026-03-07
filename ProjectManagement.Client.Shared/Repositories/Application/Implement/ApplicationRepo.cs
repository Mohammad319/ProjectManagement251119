using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Model.Application;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Client.Shared.Repositories.Application.Implement
{
    public class ApplicationRepo(HTTPRepository _httpRepository) : IApplicationRepo
    {
        static string ApplicationURLBase => PMAPIConst.Application;

        public async Task<List<ApplicationModel>> GetApplicationsAsync(bool withNoneVisible)
        {
            return await _httpRepository.GetAsync<List<ApplicationModel>>(ApplicationURLBase + $"?wnv={withNoneVisible}");
        }

        public async Task<List<ApplicationValuesModel>> GetCalcAppValuesAsync(int calcId)
        {
            return await _httpRepository.GetAsync<List<ApplicationValuesModel>>(ApplicationURLBase + URLConst.Application.AppCalculationValues + $"/{calcId}");
        }
        public async Task<int> CreateAsync(ApplicationValuesModel model)
        {
            return await _httpRepository.PostAsync<int, ApplicationValuesModel>(model, ApplicationURLBase + URLConst.Application.AppCalculationValues);
        }
        public async Task<bool> UpdateAsync(ApplicationValuesModel model)
        {
            return await _httpRepository.PutAsync(model, ApplicationURLBase + URLConst.Application.AppCalculationValues);
        }
    }
}
