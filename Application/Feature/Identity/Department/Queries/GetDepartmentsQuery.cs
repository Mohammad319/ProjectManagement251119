using Application.Interfaces;
using ProjectManagement.Shared.DTO.Identity;

namespace Application.Feature.Identity.Department.Queries
{
    public sealed record GetDepartmentsQuery() : IRequest<List<DepartmentDetailsDTO>>;

    class GetDepartmentsQueryHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<GetDepartmentsQuery, List<DepartmentDetailsDTO>>
    {
        public async Task<List<DepartmentDetailsDTO>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
        {
            return await dataAccess.Department
                    .Select(x => new DepartmentDetailsDTO
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Created = x.Created,
                        Description = x.Description,
                        LastModified = x.LastModified,
                        UsersCount = x.Users.Count,
                        ProjectsCount = x.Projects.Count,
                        FoldersCount = x.Folders.Count,
                    })
                    .ToListAsync(cancellationToken);
        }
    }
}
