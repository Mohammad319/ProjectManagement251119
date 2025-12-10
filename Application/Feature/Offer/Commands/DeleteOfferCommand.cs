using Application.Interfaces;
using System;

namespace Application.Feature.Offer.Commands
{
    public sealed record DeleteOfferCommand(int Id) : IRequest<bool>;
    public class DeleteOfferCommandHandler(IShardingSingleDbContext _dataAccess, INotificationHub notification) : IRequestHandler<DeleteOfferCommand, bool>
    {
        public async Task<bool> Handle(DeleteOfferCommand request, CancellationToken cancellationToken)
        {
            var Offer = await _dataAccess.Offers.FindAsync(request.Id);
            if (Offer == null)
                return false;
            _dataAccess.Offers.Remove(Offer);
            var resources = await _dataAccess.Resources.Where(x => x.Id ==
            Offer.ResourceId).Select(x => new
            {
                Resource = x,
                CalcID = x.Task.CalculationId
            }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
            if (resources != null && resources.Resource.PrimaryOfferId == Offer.Id)
            {
                resources.Resource.PrimaryOfferId = null;
                _dataAccess.Resources.Update(resources.Resource);
            }

            await _dataAccess.SaveChangesAsync(cancellationToken);
            if (resources != null)
                await notification.SendNotificationAsync(resources.CalcID.ToString(), ObjectTypHub.Offer, OperationType.Remove, Offer.Id);

            return true;
        }
    }
}
