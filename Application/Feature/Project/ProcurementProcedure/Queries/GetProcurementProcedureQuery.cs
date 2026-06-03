using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.ProcurementProcedure.Queries
{
    public sealed record GetProcurementProcedureQuery() : IRequest<List<ProcurementProcedureEntity>>;

    public sealed class GetProcurementProcedureQueryHandler(ILookupStatusCommandService<ProcurementProcedureEntity> service)
        : IRequestHandler<GetProcurementProcedureQuery, List<ProcurementProcedureEntity>>
    {
        public Task<List<ProcurementProcedureEntity>> Handle(GetProcurementProcedureQuery request, CancellationToken ct)
            => service.GetAllAsync(ct);
    }

    public sealed record GetVisualProcurementProcedureQuery(int? Id) : IRequest<IEnumerable<ListOrderDTO>>;

    public sealed class GetVisualProcurementProcedureQueryHandler(ILookupStatusCommandService<ProcurementProcedureEntity> service)
        : IRequestHandler<GetVisualProcurementProcedureQuery, IEnumerable<ListOrderDTO>>
    {
        public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualProcurementProcedureQuery request, CancellationToken ct)
            => service.GetVisualAsync(request.Id, ct);
    }
}
