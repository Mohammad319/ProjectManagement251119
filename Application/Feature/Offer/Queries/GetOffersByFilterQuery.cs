using Application.Interfaces;
using ProjectManagement.Shared.DTO.Offer;

namespace Application.Feature.Offer.Queries;
// -------------------------
// Get offers by filter
// -------------------------
public sealed record GetOffersByFilterQuery(OfferFilterDTO Filter) : IRequest<List<ListOfferCalcInfo>>;

public sealed class GetOffersByFilterQueryHandler(IOfferService service)
    : IRequestHandler<GetOffersByFilterQuery, List<ListOfferCalcInfo>>
{
    public Task<List<ListOfferCalcInfo>> Handle(GetOffersByFilterQuery request, CancellationToken ct)
        => service.GetByFilterAsync(request.Filter, ct);
}