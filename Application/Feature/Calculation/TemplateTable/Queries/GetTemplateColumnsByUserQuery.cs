using Application.Interfaces;
using Application.Services.CalculationItems.TemplateTable;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Feature.Calculation.TemplateTable.Queries
{
    public sealed record GetTemplateColumnsByUserQuery(int? DepartmentId)
        : IRequest<List<TemplateColumnListDTO>>;

    public sealed class GetTemplateColumnsByUserQueryHandler(ITemplateColumnQueryService service)
        : IRequestHandler<GetTemplateColumnsByUserQuery, List<TemplateColumnListDTO>>
    {
        public Task<List<TemplateColumnListDTO>> Handle(GetTemplateColumnsByUserQuery request, CancellationToken ct)
            => service.GetByUserAsync(request.DepartmentId, ct);
    }

    public sealed record GetTemplateColumnByIdQuery(int Id, int? DepartmentId)
        : IRequest<TemplateColumnModelDTO?>;

    public sealed class GetTemplateColumnByIdQueryHandler(ITemplateColumnQueryService service)
        : IRequestHandler<GetTemplateColumnByIdQuery, TemplateColumnModelDTO?>
    {
        public Task<TemplateColumnModelDTO?> Handle(GetTemplateColumnByIdQuery request, CancellationToken ct)
            => service.GetByIdAsync(request.Id, request.DepartmentId, ct);
    }
}
