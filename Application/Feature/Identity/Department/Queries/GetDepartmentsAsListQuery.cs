using Application.Interfaces;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Identity.Department.Queries
{
    public sealed record GetDepartmentsAsListQuery() : IRequest<List<ListDTO>>;
    class GetAllDepartmentsQueryHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<GetDepartmentsAsListQuery, List<ListDTO>>
    {
        public async Task<List<ListDTO>> Handle(GetDepartmentsAsListQuery request, CancellationToken cancellationToken)
        {
            return await dataAccess.Department.AsNoTracking().Select(x => new ListDTO()
            {
                Id = x.Id,
                Name = x.Name,
            })?.ToListAsync(cancellationToken: cancellationToken);
        }
    }
}
