using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Feature.Calculation.TemplateTable.Queries
{
    public sealed record GetTemplateByIdQuery(int Id, int? DepartmentId) : IRequest<TemplateModelDTO>;
    public class GetTemplateByIdQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetTemplateByIdQuery, TemplateModelDTO>
    {
        public async Task<TemplateModelDTO> Handle(GetTemplateByIdQuery query, CancellationToken cancellationToken)
        {
            return await context.Template.AsNoTracking().Where(x => x.Id == query.Id).Select(x => new TemplateModelDTO
            {
                Name = x.Name,
                Currency = x.Data.Currency,
                DateFormat = x.Data.DateFormat,
                FreezList = x.Data.FreezList,
                MathRound = x.Data.MathRound,
                NetColor = x.Data.NetColor,
                NetOrder = x.Data.NetOrder,
                NetWidth = x.Data.NetWidth,
                SSColor = x.Data.SSColor,
                SSOrder = x.Data.SSOrder,
                SSWidth = x.Data.SSWidth,
            }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        }
    }
}
