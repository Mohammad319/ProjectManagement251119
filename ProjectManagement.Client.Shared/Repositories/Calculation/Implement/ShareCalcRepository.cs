using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation.Implement
{
    public class ShareCalcRepository(HTTPRepository httpsClient) : IShareCalcRepository
    {
        private readonly HTTPRepository _httpRepository = httpsClient;
        static string ShareCalcURLBase => PMAPIConst.ShareCalc;

        public async Task<List<ListShareCalcDTO>> GetAsync(int calcId)
        {
            return await _httpRepository.GetAsync<List<ListShareCalcDTO>>(ShareCalcURLBase + calcId);
        }
        public async Task<int> CreateAsync(PostShareCalcDTO model)
        {
            return await _httpRepository.PostAsync<int, PostShareCalcDTO>(model, ShareCalcURLBase);
        }
        public async Task<bool> UpdateAsync(UpdateShareCalcDTO share)
        {
            return await _httpRepository.PutAsync(share, ShareCalcURLBase);
        }
        public async Task<bool> RemoveAsync(int id)
        {
            return await _httpRepository.DeleteAsync(ShareCalcURLBase + id);
        }
    }
}
