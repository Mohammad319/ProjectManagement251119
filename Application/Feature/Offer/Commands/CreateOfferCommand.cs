using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Hub;
using ProjectManagement.Shared.DTO.Offer;
using System;

namespace Application.Feature.Offer.Commands
{
    public sealed record CreateOfferCommand(PostOfferDTO dto) : IRequest<int>;
    public class CreateOfferCommandHandler(IShardingSingleDbContext _dataAccess, IMapper _mapper, INotificationHub notification) : IRequestHandler<CreateOfferCommand, int>
    {
        public async Task<int> Handle(CreateOfferCommand request, CancellationToken cancellationToken)
        {
            OfferEntity entity = _mapper.Map<OfferEntity>(request.dto);
            entity.Date = DateTime.Now;
            entity.Metadata = new OfferData()
            {
                BaseCost = request.dto.BaseCost,
                Contact = request.dto.Contact,
                Cost = request.dto.Cost,
            };
            _dataAccess.Offers.Add(entity);
            await _dataAccess.SaveChangesAsync(cancellationToken);
            // _dataAccess.Offers.Entry(entity).Reference(p => p.Company).Load();

            var offer = await _dataAccess.Offers.Where(x => x.Id == entity.Id).Select(x => new
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
                    Category = x.Organisation.OrganisationCategory.ParentCategory.Name,

                    //UCDepartment = x.ContactOrganisation.Department,
                    //UCMobile = x.ContactOrganisation.Mobile,
                    //UCStatus = x.ContactOrganisation.Status.ToString(),
                    //UCTelefone = x.ContactOrganisation.Telefone,
                    //ContactId = x.ContactOrganisation.Id,
                    //UCLastName = x.ContactOrganisation.LastName,
                    //UCFirstName = x.ContactOrganisation.FirstName,
                }
            }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
            await notification.SendNotificationAsync(offer.CalcID.ToString(), ObjectTypHub.Offer, OperationType.Add, new HubDataDto() { Data = offer.Offer, ParentId = entity.ResourceId });
            return entity.Id;
        }
    }
}