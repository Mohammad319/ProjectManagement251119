using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Mapping;
using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Offer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Offer
{
    public class OfferRepository(HTTPRepository _httpRepository) : IOfferRepository
    {
        static string OfferURLBase => PMAPIConst.Offer;
        //public async Task<List<OfferModel>> GetAsync(int categoryId)
        //{
        //    return await _httpRepository.GetAsync<List<OfferModel>>(OfferURLBase + URLConst.Offer.Offers + $"/{categoryId}");
        //}

        public async Task<List<ListOfferCalcInfoMVVM>> GetByFilterAsync(OfferFilterDTO model)
        {
            var dtos = await _httpRepository.PostAsync<List<ListOfferCalcInfo>, OfferFilterDTO>(model, OfferURLBase + URLConst.Offer.Filter);
            return dtos.Select(x => x.ToListOfferCalcInfoMVVM()).ToList();
        }
        public async Task<int> AddAsync(PostOfferDTO model)
        {
            return await _httpRepository.PostAsync<int, PostOfferDTO>(model, OfferURLBase + URLConst.Offer.Offers);
        }
        public async Task<bool> UpdateAsync(int id, PostOfferDTO model)
        {
            return await _httpRepository.PutAsync(model, OfferURLBase + URLConst.Offer.Offers + $"/{id}");
        }
        public async Task<bool> DeleteOfferAsync(int id)
        {
            return await _httpRepository.DeleteAsync(OfferURLBase + URLConst.Offer.Offers + "/" + id.ToString());
        }

        public async Task<bool> SetOfferToResourceAsync(int resourceId, int? offerId)
        {
            return await _httpRepository.GetAsync<bool>(OfferURLBase + URLConst.Offer.Set + $"/{resourceId}/{offerId}");
        }

        public async Task<bool> ReCalc(int calcID, int orgID, double avg)
        {
            return await _httpRepository.GetAsync<bool>(OfferURLBase + URLConst.Offer.ReCalc + $"/{calcID}/{orgID}/{avg}");
        }
    }
}
