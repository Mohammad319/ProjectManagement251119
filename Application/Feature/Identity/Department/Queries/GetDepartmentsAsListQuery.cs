using Application.Interfaces;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Identity;

namespace Application.Feature.Identity.Department.Queries
{
    public sealed record GetDepartmentsAsListQuery() : IRequest<List<ListDTO>>;
    public sealed record GetDepartmentsQuery() : IRequest<List<DepartmentDetailsDTO>>;

    public sealed class GetDepartmentsAsListQueryHandler(IDepartmentService service)
        : IRequestHandler<GetDepartmentsAsListQuery, List<ListDTO>>
    {
        public Task<List<ListDTO>> Handle(GetDepartmentsAsListQuery request, CancellationToken ct)
            => service.GetAsListAsync(ct);
    }

    public sealed class GetDepartmentsQueryHandler(IDepartmentService service)
        : IRequestHandler<GetDepartmentsQuery, List<DepartmentDetailsDTO>>
    {
        public Task<List<DepartmentDetailsDTO>> Handle(GetDepartmentsQuery request, CancellationToken ct)
            => service.GetDetailsAsync(ct);
    }
}
