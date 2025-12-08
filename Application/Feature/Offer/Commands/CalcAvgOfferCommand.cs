using Application.Interfaces;
using ProjectManagement.Shared.DTO.Hub;
using ProjectManagement.Shared.DTO.Offer;

namespace Application.Feature.Offer.Commands
{
    public sealed record CalcAvgOfferCommand(int CalcID, int OrgID, double Avg) : IRequest<bool>;

    public class CalcAvgOfferCommandHandler(IShardingSingleDbContext _dataAccess, INotificationHub notification) : IRequestHandler<CalcAvgOfferCommand, bool>
    {
        public async Task<bool> Handle(CalcAvgOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _dataAccess.Offer.Where(x => x.OrganisationId == request.OrgID &&
            x.Resource.Task.CalculationId == request.CalcID).ToListAsync(cancellationToken: cancellationToken);
            if (offer == null) return false;
            double sum = offer.Sum(c => c.Data.Cost);
            foreach (var off in offer)
            {
                off.Data.BaseCost = (off.Data.Cost * request.Avg) / sum;
            }
            _dataAccess.Offer.UpdateRange(offer);
            await _dataAccess.SaveChangesAsync(cancellationToken: cancellationToken);

            List<ListOfferDTO> result = await _dataAccess.Offer.Where(x => x.OrganisationId == request.OrgID &&
            x.Resource.Task.CalculationId == request.CalcID).Select(x => new ListOfferDTO()
            {
                Id = x.Id,
                BaseCost = x.Data.BaseCost,
                Cost = x.Data.Cost,
                Organisation = x.Organisation.Name,
                Comment = x.Comment,
                Date = x.Date,
                OrganisationId = x.OrganisationId,
                SubCategory = x.Organisation.OrganisationCategory.Name,
                Category = x.Organisation.OrganisationCategory.ParentCategory.Name
            }
            ).ToListAsync(cancellationToken: cancellationToken);

            var tt = new HubDataDto() { Data = result, ParentId = 0 };
            await notification.SendNotificationAsync(request.CalcID.ToString(), ObjectTypHub.Offer, OperationType.Update, tt);
            return true;
        }
    }
}
