using Application.Interfaces;

namespace Application.Feature.Calculation.Tender.Commands
{
    public sealed record UpdateTenderAttributeBindCommand(int TenderId, int AttributeId, double AttrValue) : IRequest<bool>;

    public class UpdateTenderAttributeBindCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<UpdateTenderAttributeBindCommand, bool>
    {
        public async Task<bool> Handle(UpdateTenderAttributeBindCommand request, CancellationToken cancellationToken)
        {
            var tender = await dataAccess.TenderAttributeBind.FirstOrDefaultAsync(x =>
            x.TenderId == request.TenderId &&
            x.TenderAttributeId == request.AttributeId);
            if (tender == null)
                return false;
            tender.Value = request.AttrValue;
            if (tender != null)
            {
                tender.Value = request.AttrValue;
                dataAccess.TenderAttributeBind.Update(tender);
            }
            else
            {
                dataAccess.TenderAttributeBind.Add(new Domain.Entities.Calculation.TenderAttributeBindEntity()
                {
                    TenderId = request.TenderId,
                    TenderAttributeId = request.AttributeId,
                    Value = request.AttrValue,
                });
            }
            await dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }

    }
}
