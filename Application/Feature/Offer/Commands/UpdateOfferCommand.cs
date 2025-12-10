using Application.Interfaces;
using ProjectManagement.Shared.DTO.Hub;
using ProjectManagement.Shared.DTO.Offer;
using System;

namespace Application.Feature.Offer.Commands
{
    public sealed record UpdateOfferCommand(PostOfferDTO dto, int Id) : IRequest<bool>;
    public class UpdateOfferCommandHandler(IShardingSingleDbContext _dataAccess, INotificationHub notification) : IRequestHandler<UpdateOfferCommand, bool>
    {
        public async Task<bool> Handle(UpdateOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _dataAccess.Offers.FindAsync(request.Id);
            if (offer == null)
                return false;
            offer.Date = DateTime.Now;
            offer.Metadata = new OfferData()
            {
                Comment = request.dto.Comment,
                BaseCost = request.dto.BaseCost,
                Contact = request.dto.Contact,
                Cost = request.dto.Cost,
            };
            offer.OrganisationId = request.dto.OrganisationId;
            await _dataAccess.SaveChangesAsync(cancellationToken);

            var result = await _dataAccess.Offers.Where(x => x.Id == offer.Id).Select(x => new
            {
                CalcID = x.Resource.Task.CalculationId,
                Offer = new ListOfferDTO()
                {
                    Id = x.Id,
                    BaseCost = x.Metadata.BaseCost,
                    Cost = x.Metadata.Cost,
                    Organisation = x.Organisation.Name,
                    Comment = x.Comment,
                    Date = x.Date,
                    OrganisationId = x.OrganisationId,
                    SubCategory = x.Organisation.OrganisationCategory.Name,
                    Category = x.Organisation.OrganisationCategory.ParentCategory.Name
                }
            }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
            List<ListOfferDTO> ll = [result.Offer];
            var tt = new HubDataDto() { Data = ll, ParentId = 0 };
            await notification.SendNotificationAsync(result.CalcID.ToString(), ObjectTypHub.Offer, OperationType.Update, tt);

            return true;
        }
    }
}
