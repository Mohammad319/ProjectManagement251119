using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Feature.Project.Project.Queries
{
    public sealed record GetProjectsGroupsByFolderQuery(Guid FolderId, bool IsVisible, int UserId, int? DepartmentId) : IRequest<IEnumerable<ListProjectDTO>>;
        public class GetProjectsGroupsByFolderHandler(IShardingSingleDbContext context) : IRequestHandler<GetProjectsGroupsByFolderQuery, IEnumerable<ListProjectDTO>>
        {
            public async Task<IEnumerable<ListProjectDTO>> Handle(GetProjectsGroupsByFolderQuery query, CancellationToken cancellationToken)
            {
                var list = context.Projects.Where(x => x.IsVisible == query.IsVisible
                && x.FolderId == query.FolderId);
                if (query.DepartmentId.HasValue)
                {
                    list = list.Where(x => x.Folder.DepartmentId == query.DepartmentId
                    || x.CreatedBy == query.UserId);
                }
                //else list = list.Where(x => x.DepartmentId.HasValue);
                return await list.AsNoTracking().Select(x => new ListProjectDTO()
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
