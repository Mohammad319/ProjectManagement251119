using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using System.Globalization;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation.Implement
{
    public class TenderRepository(HTTPRepository httpsClient) : ITenderRepository
    {
        private readonly HTTPRepository _httpRepository = httpsClient;
        static string TenderURLBase => PMAPIConst.Tender;

        public async Task<TenderDetailsDTO> DetailsAsync(int id, int calculationId)
        {
            return await _httpRepository.GetAsync<TenderDetailsDTO>(TenderURLBase + URLConst.Details + $"/{id}/{calculationId}");
        }
        public async Task<TenderAttributeValuesListDTO> GetAllAsync(int calculationId)
        {
            return await _httpRepository.GetAsync<TenderAttributeValuesListDTO>(TenderURLBase + URLConst.GetAll + $"/{calculationId}");
        }
        public async Task<int> CreateAsync(int calculationId, int companyId, TenderPostDTO model)
        {
            return await _httpRepository.PostAsync<int, TenderPostDTO>(model, TenderURLBase + $"{calculationId}/{companyId}");
        }
        public async Task<int> CreateAsync(int calculationId, TenderAttributeListPostDTO post) =>
            await _httpRepository.PostAsync<int, TenderAttributeListPostDTO>(post, TenderURLBase + URLConst.Tender.Attribute + $"/{calculationId}");
        public async Task<bool> UpdateBindAsync(int tenderID, int attrID, decimal val)
        {
            var routeValue = val.ToString(CultureInfo.InvariantCulture);
            return await _httpRepository.GetAsync<bool>(TenderURLBase + URLConst.Tender.Attribute + $"/{tenderID}/{attrID}/{routeValue}");
        }
        public async Task<bool> UpdateAsync(int id, int calculationId, TenderPostDTO model)
        {
            return await _httpRepository.PutAsync(model, TenderURLBase + $"{id}/{calculationId}");
        }
        public async Task<bool> UpdateAsync(int id, int calculationId, TenderAttributeListPostDTO post)
        => await _httpRepository.PutAsync(post, TenderURLBase + URLConst.Tender.Attribute + $"/{id}/{calculationId}");
        
        public async Task<bool> DeleteAsync(int id, int calculationId)
            => await _httpRepository.DeleteAsync(TenderURLBase + $"{id}/{calculationId}");
        public async Task<bool> DeleteAttributeAsync(int id, int calculationId) =>
        await _httpRepository.DeleteAsync(TenderURLBase + URLConst.Tender.Attribute + $"/{id}/{calculationId}");
    }
}
