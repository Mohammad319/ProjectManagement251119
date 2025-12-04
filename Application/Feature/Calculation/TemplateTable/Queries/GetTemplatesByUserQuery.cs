using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Feature.Calculation.TemplateTable.Queries
{
    public sealed record GetTemplatesByUserQuery(int? DepartmentId) : IRequest<List<TemplateListDTO>>;
    public class GetTemplatesByUserQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetTemplatesByUserQuery, List<TemplateListDTO>>
    {
        public async Task<List<TemplateListDTO>> Handle(GetTemplatesByUserQuery query, CancellationToken cancellationToken)
        {
            return await context.Template.AsNoTracking().Where(x => x.DepartmentId == query.DepartmentId).OrderByDescending(x => x)
                 .Select(x => new TemplateListDTO { Id = x.Id, Name = x.Name }).ToListAsync(cancellationToken: cancellationToken);
        }
    }
}
