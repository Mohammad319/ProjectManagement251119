using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Feature.Project.Project.Queries
{
    public sealed record GetProjectsOtherGroupByFolderQuery(Guid FolderId, int UserId, int? DepartmentId) : IRequest<IEnumerable<ListProjectDTO>>;
    public class GetProjectsByFolderQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetProjectsOtherGroupByFolderQuery, IEnumerable<ListProjectDTO>>
    {
        public async Task<IEnumerable<ListProjectDTO>> Handle(GetProjectsOtherGroupByFolderQuery query, CancellationToken cancellationToken)
        {
            return await context.Projects.Where(x => x.IsVisible == true && x.FolderId == query.FolderId &&
            x.Calculations.SelectMany(c => c.SharesCalc).Any(s => s.CreatedBy == query.UserId ||
            s.DepartmentId == query.DepartmentId)).AsNoTracking().Select(x => new ListProjectDTO()
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                EndDate = x.EndDate,
                StartDate = x.StartDate,
                Order = x.SortOrder,
            }).ToListAsync(cancellationToken);
        }
    }
}
