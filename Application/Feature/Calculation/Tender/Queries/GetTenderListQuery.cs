using Application.Interfaces;
using Application.Interfaces.Context;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.Calculation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Calculation.Tender.Queries
{
    public class GetTenderListQuery : IRequest<TenderAttributeValuesListDTO>
    {
        public int CalculationId { get; set; }

        public class GetTenderListQueryHandler : IRequestHandler<GetTenderListQuery, TenderAttributeValuesListDTO>
        {
            private readonly IShardingSingleDbContext _context;

            public GetTenderListQueryHandler(IShardingSingleDbContext context)
            {
                _context = context;
            }
            public async Task<TenderAttributeValuesListDTO> Handle(GetTenderListQuery query, CancellationToken cancellationToken)
            {
                TenderAttributeValuesListDTO result = new();
                result.Tenders = await _context.Tender.Where(x => x.CalculationId == query.CalculationId).Select(x => new TenderListDTO()
                {
                    Id = x.Id,
                    CompanyId = x.OrganisationId,
                    Company = x.Organisation.Name,
                    Category = x.Organisation.OrganisationCategory.Name,
                    SubCategory = x.Organisation.OrganisationCategory.ParentCategory.Name,
                    Values = x.TendersAttributes.Select((a) => new ValuesList()
                    {
                        AttributeID = a.TenderAttributeId,
                        Values = a.Value
                    }).ToList(),
                }).AsNoTracking().ToListAsync();

                result.Attributes = await _context.AttributeNameTender
                    .Where(x => x.CalculationId == query.CalculationId)
                    .Select(x => new TenderAttributeListDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Note = x.Note,
                    }).AsNoTracking().ToListAsync();

                return result;
            }
        }
    }
}
