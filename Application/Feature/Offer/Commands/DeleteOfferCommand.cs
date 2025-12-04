using Application.Interfaces;
using System;

namespace Application.Feature.Offer.Commands
{
    public sealed record DeleteOfferCommand(int Id) : IRequest<bool>;
    public class DeleteOfferCommandHandler(IShardingSingleDbContext _dataAccess, INotificationHub notification) : IRequestHandler<DeleteOfferCommand, bool>
    {
        public async Task<bool> Handle(DeleteOfferCommand request, CancellationToken cancellationToken)
        {
            var Offer = await _dataAccess.Offer.FindAsync(request.Id);
            if (Offer == null)
                return false;
            _dataAccess.Offer.Remove(Offer);
            var resources = await _dataAccess.Resource.Where(x => x.Id ==
            Offer.ResourceID).Select(x => new
            {
                Resource = x,
                CalcID = x.Task.CalculationId
            }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
            if (resources != null && resources.Resource.OfferId == Offer.Id)
            {
                resources.Resource.OfferId = null;
                _dataAccess.Resource.Update(resources.Resource);
            }

            await _dataAccess.SaveChangesAsync(cancellationToken);
            if (resources != null)
                await notification.SendNotificationAsync(resources.CalcID.ToString(), ObjectTypHub.Offer, OperationType.Remove, Offer.Id);

            return true;
        }
    }
}
