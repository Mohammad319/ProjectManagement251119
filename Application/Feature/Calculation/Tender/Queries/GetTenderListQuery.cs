using Application.Interfaces;
using Application.Services.CalculationItems.Tender;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Tender.Queries
{
    // =========================================================
    // Tender Queries
    // =========================================================

    // --------- GetTenderList ---------
    public sealed record GetTenderListQuery(
        int CalculationId
    ) : IRequest<List<TenderListDTO>>;

    public sealed class GetTenderListQueryHandler
        : IRequestHandler<GetTenderListQuery, List<TenderListDTO>>
    {
        private readonly ITenderQueryService _service;

        public GetTenderListQueryHandler(ITenderQueryService service)
        {
            _service = service;
        }

        public Task<List<TenderListDTO>> Handle(GetTenderListQuery request, CancellationToken cancellationToken)
            => _service.GetTenderListAsync(request.CalculationId, cancellationToken);
    }

    // --------- GetTenderDetails ---------
    public sealed record GetTenderDetailsQuery(
        int TenderId,int calculationId
    ) : IRequest<TenderDetailsDTO?>;

    public sealed class GetTenderDetailsQueryHandler(ITenderQueryService service)
                : IRequestHandler<GetTenderDetailsQuery, TenderDetailsDTO?>
    {
        public Task<TenderDetailsDTO?> Handle(GetTenderDetailsQuery request, CancellationToken cancellationToken)
            => service.GetTenderDetailsAsync(request.TenderId,request.calculationId, cancellationToken);
    }

    // =========================================================
    // TenderAttribute Queries
    // =========================================================

    // --------- GetTenderAttributes (per Calculation) ---------
    public sealed record GetTenderAttributesQuery(
        int CalculationId
    ) : IRequest<List<TenderAttributeListDTO>>;

    public sealed class GetTenderAttributesQueryHandler
        : IRequestHandler<GetTenderAttributesQuery, List<TenderAttributeListDTO>>
    {
        private readonly ITenderAttributeQueryService _service;

        public GetTenderAttributesQueryHandler(ITenderAttributeQueryService service)
        {
            _service = service;
        }

        public Task<List<TenderAttributeListDTO>> Handle(GetTenderAttributesQuery request, CancellationToken cancellationToken)
            => _service.GetAttributesAsync(request.CalculationId, cancellationToken);
    }
}
