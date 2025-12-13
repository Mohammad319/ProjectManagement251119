//using Application.Interfaces;
//using Domain.Entities.Calculation;
//using ProjectManagement.Shared.DTO.General;
//using System;
//using System.Linq.Expressions;

//namespace Application.Feature.Project.StatusResource.Queries;

//public sealed record GetVisualStatusResourceQuery(int? Id) : IRequest<IEnumerable<ListOrderDTO>>;

//public sealed class GetVisualStatusResourceQueryHandler(IShardingSingleDbContext _context)
//    : IRequestHandler<GetVisualStatusResourceQuery, IEnumerable<ListOrderDTO>>
//{
//    public async Task<IEnumerable<ListOrderDTO>> Handle(GetVisualStatusResourceQuery request, CancellationToken cancellationToken)
//    {
//        Expression<Func<StatusResourcesEntity, bool>> predicate =
//            request.Id.HasValue
//                ? x => x.IsVisible || x.Id == request.Id
//                : x => x.IsVisible;

//        return await _context.ResourceStatus
//            .Where(predicate)
//            .OrderByDescending(x => x)
//            .AsNoTracking()
//            .Select(x => new ListOrderDTO
//            {
//                Id = x.Id,
//                Name = x.Name,
//                Color = x.Color
//            })
//            .ToListAsync(cancellationToken);
//    }
//}
