using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Tender.Commands
{
    public sealed record CreateTenderCommand(TenderPostDTO dto, int CalculationId, int CompanyId) : IRequest<int>;

    public class CreateTenderCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<CreateTenderCommand, int>
    {
        public async Task<int> Handle(CreateTenderCommand request, CancellationToken cancellationToken)
        {
            TenderEntity tender = new()
            {
                CalculationId = request.CalculationId,
                OrganisationId = request.CompanyId,
                Note = request.dto.Note,
            };

            dataAccess.Tenders.Add(tender);
            await dataAccess.SaveChangesAsync();

            List<TenderAttributeBindEntity> TendersAttributes = [];
            foreach (var attr in request.dto.AttributesValue)
                TendersAttributes.Add(new TenderAttributeBindEntity()
                {
                    TenderId = tender.Id,
                    TenderAttributeId = attr.Key,
                    Value = attr.Value
                });

            dataAccess.TenderAttributeBind.AddRange(TendersAttributes);
            await dataAccess.SaveChangesAsync(cancellationToken);

            return tender.Id;
        }
    }
}
