using Application.Interfaces;
using ProjectManagement.Shared.DTO.Folder;

namespace Application.Feature.Project.Folder.Queries
{
    public sealed record GetFoldersFromOtherDepartmentQuery(int DepartmentId) : IRequest<IEnumerable<ListFolderDTO>>;
    public class GetFoldersFromOtherDepartmentQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetFoldersFromOtherDepartmentQuery, IEnumerable<ListFolderDTO>>
    {
        public async Task<IEnumerable<ListFolderDTO>> Handle(GetFoldersFromOtherDepartmentQuery query, CancellationToken cancellationToken)
        {
            return await context.Folders.Where(x => x.IsVisible == true &&
            x.DepartmentId == query.DepartmentId).AsNoTracking().Select(x => new ListFolderDTO
            {
                Color = x.Color,
                Name = x.Name,
                Id = x.Id,
                Order = x.SortOrder,
            }).ToListAsync(cancellationToken: cancellationToken);
        }
    }
}
