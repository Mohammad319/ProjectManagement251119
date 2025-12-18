using Application.Interfaces;
using Domain.DTO.User;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Identity;

namespace Application.Feature.Identity.Department.Queries
{
    public sealed record GetDepartmentsAsListQuery() : IRequest<List<ListDTO>>;
    public sealed record GetDepartmentsQuery() : IRequest<List<DepartmentDetailsDTO>>;
    public sealed record GetUserssQuery(int? departmentId) : IRequest<List<TenantUserDto>>;
    public sealed class GetUserssQueryHandler(IDepartmentService service)
    : IRequestHandler<GetUserssQuery, List<TenantUserDto>>
    {
        public Task<List<TenantUserDto>> Handle(GetUserssQuery request, CancellationToken ct)
            => service.GetUsersByDepartmentIdAsync(request.departmentId, ct);
    }
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
