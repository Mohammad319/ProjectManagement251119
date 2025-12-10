using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Feature.Calculation.TemplateTable.Queries
{
    public sealed record GetTemplateByIdQuery(int Id, int? DepartmentId) : IRequest<TemplateModelDTO>;
    public class GetTemplateByIdQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetTemplateByIdQuery, TemplateModelDTO>
    {
        public async Task<TemplateModelDTO> Handle(GetTemplateByIdQuery query, CancellationToken cancellationToken)
        {
            return await context.Templates.AsNoTracking().Where(x => x.Id == query.Id).Select(x => new TemplateModelDTO
            {
                Name = x.Name,
                Currency = x.Metadata.Currency,
                DateFormat = x.Metadata.DateFormat,
                FreezList = x.Metadata.FreezList,
                MathRound = x.Metadata.MathRound,
                NetColor = x.Metadata.NetColor,
                NetOrder = x.Metadata.NetOrder,
                NetWidth = x.Metadata.NetWidth,
                SSColor = x.Metadata.SSColor,
                SSOrder = x.Metadata.SSOrder,
                SSWidth = x.Metadata.SSWidth,
            }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        }
    }
}
