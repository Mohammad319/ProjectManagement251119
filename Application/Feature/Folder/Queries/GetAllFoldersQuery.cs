using Application.Interfaces;
using ProjectManagement.Shared.DTO.Folder;

namespace Application.Feature.Project.Folder.Queries
{
    public sealed record GetAllFoldersQuery() : IRequest<IEnumerable<ListFolderDTO>>;
    public class GetAllFoldersQueryHandler(IShardingSingleDbContext _context) : IRequestHandler<GetAllFoldersQuery, IEnumerable<ListFolderDTO>>
    {
        public async Task<IEnumerable<ListFolderDTO>> Handle(GetAllFoldersQuery query, CancellationToken cancellationToken)
        {
            return await _context.Folder.Where(x => x.IsVisible == true)
                .AsNoTracking().Select(x => new ListFolderDTO
                {
                    Color = x.Color,
                    Name = x.Name,
                    Id = x.Id,
                    Order = x.SortOrder,
                }).ToListAsync(cancellationToken: cancellationToken);
        }
    }
}
