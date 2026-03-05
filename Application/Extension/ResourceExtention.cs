using Domain.Entities.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Offer;

namespace Application.Extention
{
    public static class ResourceExtention
    {
        public static ResourceListDTO MapToResourceListDTO(this ResourceEntity r)
        {
            // ✅ Hydrate duplicated fields from scalar columns to keep UI consistent
            // (Note/Unit exist both as columns and inside JSON metadata)
            var data = r.Metadata?.Clone() ?? new ResourceMetadata();

            if (!string.IsNullOrWhiteSpace(r.Note))
                data.Note = r.Note!;

            if (!string.IsNullOrWhiteSpace(r.Unit))
                data.Unit = r.Unit!;

            return new ResourceListDTO
            {
                Name = r.Name,
                Active = r.IsActive,
                Id = r.Id,
                RowVersion = r.RowVersion,
                ResType = r.ResType,
                ResourceSortId = r.ResourceSortId,
                ResourceTypeId = r.ResourceTypeId,
                AccountId = r.AccountId,
                StatusId = r.StatusId,
                OfferId = r.PrimaryOfferId,
                Order = r.SortOrder,
                OpportunityId = r.OpportunityId,
                Data = data,
                Opportunity = r.Opportunity?.OpportunityType ?? string.Empty,
                StatusColor = r.Status?.Color ?? string.Empty,
                Status = r.Status?.Name ?? string.Empty,
                Sort = r.ResourceSort?.Name ?? string.Empty,
                ResName = r.ResourceType?.Name ?? string.Empty,
                Account = r.Account?.Name ?? string.Empty,
                AccountCode = r.Account?.Code ?? string.Empty,
                TaskId = r.TaskId,
                Offers = r.Offers == null ? [] : r.Offers.Select(MapToListOfferDTO).ToList(),
            };
        }

        public static ListOfferDTO MapToListOfferDTO(OfferEntity of)
        {
            return new ListOfferDTO
            {
                Id = of.Id,
                RowVersion = of.RowVersion,
                BaseCost = of.Metadata.BaseCost,
                Cost = of.Metadata.Cost,
                Comment = of.Comment ?? string.Empty,
                Date = of.Date,
                OrganisationId = of.OrganisationId,
                Organisation = of.Organisation != null ? of.Organisation.Name : string.Empty,
                SubCategory = of.Organisation != null && of.Organisation.OrganisationCategory != null ? of.Organisation.OrganisationCategory.Name : string.Empty,
                Category = of.Organisation != null && of.Organisation.OrganisationCategory != null && of.Organisation.OrganisationCategory.ParentCategory != null ? of.Organisation.OrganisationCategory.ParentCategory.Name : string.Empty
            };
        }

        public static ResourceEntity Parse(this ResourcePostDTO res, int taskID)
        {
            // ✅ ensure Data carries Note/Unit (DTO proxies already do, but keep it explicit)
            res.Data.Note = res.Note ?? res.Data.Note;
            res.Data.Unit = res.Unit ?? res.Data.Unit;

            return new ResourceEntity()
            {
                StatusId = res.StatusId,
                AccountId = res.AccountId,
                PrimaryOfferId = res.OfferId,
                OpportunityId = res.OpportunityId,
                ResourceSortId = res.ResourceSortId,
                ResourceTypeId = res.ResourceTypeId,
                TaskId = taskID,
                ResType = res.ResType,
                IsActive = res.IsActive,
                Name = res.Name,
                SortOrder = res.SortOrder,
                Note = res.Note,
                Unit = res.Unit,
                Metadata = res.Data,
            };
        }

        public static ResourceEntity Reset(ResourceEntity res)
        {
            return new ResourceEntity()
            {
                Name = res.Name,
                Metadata = res.Metadata,
                IsActive = res.IsActive,
                ResType = res.ResType,
                TenantId = res.TenantId,
            };
        }
    }
}
