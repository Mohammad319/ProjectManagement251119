using Application.Interfaces;

namespace Application.Feature.Calculation.Tender.Commands
{
    public sealed record DeleteTenderCommand(int Id, int CalculationId) : IRequest<bool>;
    public class DeleteTenderCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<DeleteTenderCommand, bool>
    {
        public async Task<bool> Handle(DeleteTenderCommand request, CancellationToken cancellationToken)
        {
            var tenders = await dataAccess.TenderAttributeBind.Where(x => x.TenderId == request.Id).ToListAsync();
            if (tenders != null)
            {
                dataAccess.TenderAttributeBind.RemoveRange(tenders);
                await dataAccess.SaveChangesAsync();
            }
            var tender = await dataAccess.Tender.FirstOrDefaultAsync(x => x.Id == request.Id &&
            x.CalculationId == request.CalculationId);
            if (tender == null)
                return false;
            dataAccess.Tender.Remove(tender);
            await dataAccess.SaveChangesAsync();
            return true;
        }

    }
}
