using Application.Feature.Project.Project;
using Application.Interfaces;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.Type.Queries
{
    public class GetProjectCalcConfigQuery : IRequest<GetProjectCalcConfigDTO>
    {
        public int TypeObj;

        public int Methods;
        public int Contracts;
        public int Compensations;
        public int Types;
        public int Statuses;
        public int OrgId;

    }
    public sealed class GetProjectCalcConfigQueryHandler(IProjectService service)
       : IRequestHandler<GetProjectCalcConfigQuery, GetProjectCalcConfigDTO>
    {
        public Task<GetProjectCalcConfigDTO> Handle(GetProjectCalcConfigQuery request, CancellationToken ct)
            => service.GetProjectCalcConfigAsync(
                request.TypeObj,
                request.Methods,
                request.Contracts,
                request.Compensations,
                request.Types,
                request.Statuses,
                request.OrgId,
                ct);
    }

}
