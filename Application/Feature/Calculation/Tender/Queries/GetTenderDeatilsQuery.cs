using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Tender.Queries
{
    public sealed record GetTenderDeatilsQuery(int Id, int CalculationId) : IRequest<TenderDetailsDTO>;
    public class GetTenderDeatilsQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetTenderDeatilsQuery, TenderDetailsDTO>
    {
        public async Task<TenderDetailsDTO> Handle(GetTenderDeatilsQuery query, CancellationToken cancellationToken)
        {
            return await context.Tender.Where(x => x.Id == query.Id && x.CalculationId == query.CalculationId).Select(
                x => new TenderDetailsDTO()
                {
                    Id = x.Id,
                    Company = x.Organisation.Name,
                    Category = x.Organisation.Category.Name,
                    SubCategory = x.Organisation.Category.Category.Name,
                    Note = x.Note,
                }
                ).AsNoTracking().FirstOrDefaultAsync();
        }
    }
}
