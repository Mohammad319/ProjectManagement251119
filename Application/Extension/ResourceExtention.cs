using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Offer;
using System.Linq;

namespace Application.Extention
{
    public static class ResourceExtention
    {
        public static ResourceListDTO MapToResourceListDTO(this ResourceEntity r)
        {
            return new ResourceListDTO
            {
                Name = r.Name,
                Active = r.Active,
                Id = r.Id,
                ResType = r.ResType,
                ResourceSortId = r.ResourceSortId,
                ResourceTypeId = r.ResourceTypeId,
                AccountId = r.AccountId,
                StatusId = r.StatusId,
                OfferId = r.OfferId,
                Order = r.Order,
                OpportunityId = r.OpportunityId,
                Data = r.Data,
                Opportunity = r.Opportunity?.Type ?? string.Empty,
                StatusColor = r.Status?.Color ?? string.Empty,
                Status = r.Status?.Name ?? string.Empty,
                Sort = r.ResourceSort?.Name ?? string.Empty,
                ResName = r.ResourceType?.Name ?? string.Empty,
                Account = r.Account?.Name ?? string.Empty,
                AccountCode = r.Account?.Account ?? string.Empty,
                TaskId = r.TaskId,
                Offers = r.Offers == null ? []: r.Offers.Select(of => MapToListOfferDTO(of)).ToList(),
            };
        }
        public static ListOfferDTO MapToListOfferDTO(OfferEntity of)
        {
            return new ListOfferDTO
            {
                Id = of.Id,
                BaseCost = of.Data.BaseCost,
                Cost = of.Data.Cost,
                Comment = of.Data.Comment,
                Date = of.Date,
                OrganisationId = of.OrganisationId,
                Organisation = of.Organisation != null ? of.Organisation.Name : string.Empty,
                SubCategory = of.Organisation != null && of.Organisation.Category != null ? of.Organisation.Category.Name : string.Empty,
                Category = of.Organisation != null && of.Organisation.Category != null && of.Organisation.Category.Category != null ? of.Organisation.Category.Category.Name : string.Empty
            };
        }

        public static ResourceEntity Parse(this ResourcePostDTO res, int taskID)
        {
            return new ResourceEntity()
            {
                StatusId = res.StatusId,
                AccountId = res.AccountId,
                OfferId = res.OfferId,
                OpportunityId = res.OpportunityId,
                ResourceSortId = res.ResourceSortId,
                ResourceTypeId = res.ResourceTypeId,
                TaskId = taskID,
                ResType = res.ResType,
                Active = res.Active,
                Name = res.Name,
                Order = res.Order,
                Data = res.Data,
            };
        }
        public static ResourceEntity Reset(ResourceEntity res)
        {
            return new ResourceEntity()
            {
                Name = res.Name,
                Data = res.Data,
                Active = res.Active,
                ResType = res.ResType,
                TenantId = res.TenantId,
            };
        }
    }
}
