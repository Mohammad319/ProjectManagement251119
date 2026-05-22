using ProjectManagement.Shared.DTO.Offer;

namespace Application.Feature.Offer
{
    public interface IOfferService
    {
        // Commands
        Task<int> CreateAsync(PostOfferDTO dto, int? departmentId, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, PostOfferDTO dto, int? departmentId, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, int? departmentId, CancellationToken ct = default);
        Task<bool> SetPrimaryOfferAsync(int resourceId, int? offerId, int? departmentId, CancellationToken ct = default);
        Task<bool> CalcAvgOfferAsync(int calcId, int organisationId, double avg, int? departmentId, CancellationToken ct = default);

        // Queries
        Task<List<ListOfferCalcInfo>> GetByFilterAsync(OfferFilterDTO filter, int? departmentId, CancellationToken ct = default);
    }
}
