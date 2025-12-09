using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.General;
using System;
using System.Linq.Expressions;

namespace Application.Feature.Project.TaskStatus.Queries
{
    public sealed record GetVisualTaskStatusQuery(int? ID) : IRequest<IEnumerable<ListOrderDTO>>;
    public class GetVisualTaskStatusQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetVisualTaskStatusQuery, IEnumerable<ListOrderDTO>>
    {
        private readonly IShardingSingleDbContext _context = context;

        public async Task<IEnumerable<ListOrderDTO>> Handle(GetVisualTaskStatusQuery query, CancellationToken cancellationToken)
        {
            Expression<Func<TaskStatusEntity, bool>> predicate;
            if (query.ID.HasValue)
                predicate = x => x.IsVisible == true || x.Id == query.ID;
            else
                predicate = x => x.IsVisible == true;
            return await _context.TaskStatus.Where(predicate)
                .OrderByDescending(x => x).Select(x => new ListOrderDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Order = x.SortOrder,
                    Color = x.Color,
                }).AsNoTracking().ToListAsync(cancellationToken: cancellationToken);
        }
    }
}
