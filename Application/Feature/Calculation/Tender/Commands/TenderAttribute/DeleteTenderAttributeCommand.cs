using Application.Interfaces;

namespace Application.Feature.Calculation.Tender.Commands
{
    public sealed record DeleteTenderAttributeCommand(int Id, int CalculationId) : IRequest<bool>;

    public class DeleteTenderAttributeCommandHandler : IRequestHandler<DeleteTenderAttributeCommand, bool>
    {
        private readonly IShardingSingleDbContext _dataAccess;

        public DeleteTenderAttributeCommandHandler(IShardingSingleDbContext dataAccess)
        {
            _dataAccess = dataAccess;
        }
        public async Task<bool> Handle(DeleteTenderAttributeCommand request, CancellationToken cancellationToken)
        {
            var tenders = await _dataAccess.TenderAttributeBind.Where(x => x.TenderAttributeId == request.Id).ToListAsync();
            if (tenders != null)
            {
                _dataAccess.TenderAttributeBind.RemoveRange(tenders);
                await _dataAccess.SaveChangesAsync();
            }

            var tender = await _dataAccess.AttributeNameTender.FirstOrDefaultAsync(x => x.Id == request.Id &&
            x.CalculationId == request.CalculationId);
            if (tender == null)
                return false;
            _dataAccess.AttributeNameTender.Remove(tender);
            await _dataAccess.SaveChangesAsync();
            return true;
        }

    }
}
