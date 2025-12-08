using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Feature.Project.Project.Queries
{
    public sealed record GetMyProjectsQuery(Guid FolderId, int UserId, bool IsVisible) : IRequest<IEnumerable<ListProjectDTO>>;
    public class GetMyProjectsQueryHandler(IShardingSingleDbContext _context) : IRequestHandler<GetMyProjectsQuery, IEnumerable<ListProjectDTO>>
    {
        public async Task<IEnumerable<ListProjectDTO>> Handle(GetMyProjectsQuery query, CancellationToken cancellationToken)
        {
            return await _context.Project.OrderByDescending(x => x)
                .Where(x => x.IsVisible == query.IsVisible && x.UserId == query.UserId)
                .AsNoTracking().Select(x => new ListProjectDTO()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    Order = x.SortOrder,
                    EndDate = x.EndDate,
                    StartDate = x.StartDate,
                }).ToListAsync(cancellationToken);
        }
    }
}
