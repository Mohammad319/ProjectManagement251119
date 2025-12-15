using ProjectManagement.Shared.DTO.Offer;

namespace Application.Feature.Offer
{
    public interface IOfferService
    {
        // Commands
        Task<int> CreateAsync(PostOfferDTO dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, PostOfferDTO dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
        Task<bool> SetPrimaryOfferAsync(int resourceId, int? offerId, CancellationToken ct = default);
        Task<bool> CalcAvgOfferAsync(int calcId, int organisationId, double avg, CancellationToken ct = default);

        // Queries
        Task<List<ListOfferCalcInfo>> GetByFilterAsync(OfferFilterDTO filter, CancellationToken ct = default);
    }
}
