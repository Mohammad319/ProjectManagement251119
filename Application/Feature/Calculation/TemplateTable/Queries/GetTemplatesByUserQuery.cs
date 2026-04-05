using Application.Interfaces;
using Application.Services.CalculationItems.TemplateTable;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Feature.Calculation.TemplateTable.Queries
{
    // ============================================
    // GetTemplateByIdQuery
    // ============================================

    public sealed record GetTemplateByIdQuery(
        int Id,
        int? DepartmentId
    ) : IRequest<TemplateModelDTO?>;

    public sealed class GetTemplateByIdQueryHandler(ITemplateQueryService service)
                : IRequestHandler<GetTemplateByIdQuery, TemplateModelDTO?>
    {
        public Task<TemplateModelDTO?> Handle(
            GetTemplateByIdQuery request,
            CancellationToken cancellationToken)
            => service.GetByIdAsync(request.Id, request.DepartmentId, cancellationToken);
    }

    // ============================================
    // GetTemplatesByUserQuery
    // ============================================

    public sealed record GetTemplatesByUserQuery(
        int? DepartmentId
    ) : IRequest<List<TemplateListDTO>>;

    public sealed class GetTemplatesByUserQueryHandler
        : IRequestHandler<GetTemplatesByUserQuery, List<TemplateListDTO>>
    {
        private readonly ITemplateQueryService _service;

        public GetTemplatesByUserQueryHandler(ITemplateQueryService service)
        {
            _service = service;
        }

        public Task<List<TemplateListDTO>> Handle(
            GetTemplatesByUserQuery request,
            CancellationToken cancellationToken)
            => _service.GetByUserAsync(request.DepartmentId, cancellationToken);
    }
}
