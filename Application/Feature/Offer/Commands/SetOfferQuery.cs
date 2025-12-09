using Application.Extention;
using Application.Interfaces;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Hub;
using System;

namespace Application.Feature.Offer.Commands
{
    public sealed record DeleteFolderCommand(int ResourceId, int? OfferId) : IRequest<bool>;

    public class GetSetOfferQuery : IRequest<bool>
    {
        public int ResourceId;
        public int? OfferId;

        public class GetSetOfferQueryHandler(IShardingSingleDbContext _context, INotificationHub notification) : IRequestHandler<GetSetOfferQuery, bool>
        {
            public async Task<bool> Handle(GetSetOfferQuery query, CancellationToken cancellationToken)
            {
                if (query.OfferId == 0) query.OfferId = null;
                var item = await _context.Resource.Where(x => x.Id == query.ResourceId)
    .Select(x => new
    {
        Resource = x,
        Offer = query.OfferId.HasValue ? x.Offers.FirstOrDefault(o => o.Id == query.OfferId) : null,
        CalcID = x.Task.CalculationId,
    }).FirstOrDefaultAsync(cancellationToken);

                if (item == null || (query.OfferId.HasValue && item.Offer == null)) return false;
                item.Resource.OfferId = query.OfferId;
                if (item.Offer != null)
                {
                    item.Resource.Metadata.BaseCost = item.Offer.Data.BaseCost;
                    item.Resource.Metadata.Cost = item.Offer.Data.Cost;
                }

                _context.Resource.Update(item.Resource);
                await _context.SaveChangesAsync(cancellationToken);
                Console.WriteLine("OfferId " + query.OfferId.ToString());
                await notification.SendNotificationAsync(item.CalcID.ToString(), ObjectTypHub.Offer, OperationType.Update, new HubDataDto() { Parent = query.OfferId.ToString(), ParentId = item.Resource.Id });
                return true;
            }
        }
    }
}
