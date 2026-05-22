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
        , int? DepartmentId
    ) : IRequest<TenderAttributeValuesListDTO>;

    public sealed class GetTenderListQueryHandler(ITenderQueryService service)
                : IRequestHandler<GetTenderListQuery, TenderAttributeValuesListDTO>
    {
        public Task<TenderAttributeValuesListDTO> Handle(GetTenderListQuery request, CancellationToken cancellationToken)
            => service.GetTenderListAsync(request.CalculationId, request.DepartmentId, cancellationToken);
    }

    // --------- GetTenderDetails ---------
    public sealed record GetTenderDetailsQuery(
        int TenderId,int calculationId, int? DepartmentId
    ) : IRequest<TenderDetailsDTO?>;

    public sealed class GetTenderDetailsQueryHandler(ITenderQueryService service)
                : IRequestHandler<GetTenderDetailsQuery, TenderDetailsDTO?>
    {
        public Task<TenderDetailsDTO?> Handle(GetTenderDetailsQuery request, CancellationToken cancellationToken)
            => service.GetTenderDetailsAsync(request.TenderId,request.calculationId, request.DepartmentId, cancellationToken);
    }

    // =========================================================
    // TenderAttribute Queries
    // =========================================================

    // --------- GetTenderAttributes (per Calculation) ---------
    public sealed record GetTenderAttributesQuery(
        int CalculationId
        , int? DepartmentId
    ) : IRequest<List<TenderAttributeListDTO>>;

    public sealed class GetTenderAttributesQueryHandler(ITenderAttributeQueryService service)
                : IRequestHandler<GetTenderAttributesQuery, List<TenderAttributeListDTO>>
    {
        public Task<List<TenderAttributeListDTO>> Handle(GetTenderAttributesQuery request, CancellationToken cancellationToken)
            => service.GetAttributesAsync(request.CalculationId, request.DepartmentId, cancellationToken);
    }
}
