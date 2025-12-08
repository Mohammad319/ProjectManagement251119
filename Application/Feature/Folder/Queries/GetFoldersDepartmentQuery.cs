using Application.Interfaces;
using ProjectManagement.Shared.DTO.Folder;

namespace Application.Feature.Project.Folder.Queries
{
    public sealed record GetFoldersDepartmentQuery(bool IsVisible, int? DepartmentId) : IRequest<List<ListFolderDTO>>;

    public class GetFoldersMyDepartmentQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetFoldersDepartmentQuery, List<ListFolderDTO>>
    {
        public async Task<List<ListFolderDTO>> Handle(GetFoldersDepartmentQuery query, CancellationToken cancellationToken)
        {
            var list = context.Folder.Where(x => x.IsVisible == query.IsVisible).AsNoTracking().AsQueryable();

            if (query.DepartmentId.HasValue)
            {
                list = list.Where(x => x.DepartmentId == query.DepartmentId);
            }

            return await list.Select(x => new ListFolderDTO
            {
                Color = x.Color,
                Name = x.Name,
                Id = x.Id,
                Order = x.SortOrder,
            }).ToListAsync(cancellationToken: cancellationToken);
        }
    }
}
