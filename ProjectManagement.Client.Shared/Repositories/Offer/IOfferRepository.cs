using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Shared.DTO.Offer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Offer
{
    public interface IOfferRepository
    {
        Task<List<ListOfferCalcInfoMVVM>> GetByFilterAsync(OfferFilterDTO filter);
        Task<bool> SetOfferToResourceAsync(int resourceId, int? offerId);
        Task<bool> ReCalc(int calcID, int orgID, double avg);
        Task<int> AddAsync(PostOfferDTO offer);
        Task<bool> UpdateAsync(int id, PostOfferDTO offer);
        Task<bool> DeleteOfferAsync(int id);
    }
}
