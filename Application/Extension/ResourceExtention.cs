using Domain.Entities.Calculation;
using Application.Mapping.Offer;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Offer;

namespace Application.Extention
{
    public static class ResourceExtention
    {
        public static ResourceListDTO MapToResourceListDTO(this ResourceEntity r)
        {
            var data = r.GetMetadataSnapshot();

            return new ResourceListDTO
            {
                Name = r.Name,
                IsActive = r.IsActive,
                Id = r.Id,
                RowVersion = r.RowVersion,
                ResType = r.ResType,
                ResourceSortId = r.ResourceSortId,
                ResourceTypeId = r.ResourceTypeId,
                AccountId = r.AccountId,
                StatusId = r.StatusId,
                OfferId = r.PrimaryOfferId,
                SortOrder = r.SortOrder,
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
            return of.ToListDto();
        }

        public static ResourceEntity Parse(this ResourcePostDTO res, int taskID)
        {
            res.Data = CalculationItemMetadataMapper.BuildResourceMetadata(
                res.Data,
                res.Note,
                res.Unit);

            return ResourceEntity.Create(res, res.SortOrder, taskID > 0 ? taskID : null);
        }

        public static ResourceEntity Reset(ResourceEntity res)
        {
            var clone = ResourceEntity.CloneForTask(res);
            clone.TenantId = res.TenantId;
            clone.ResetIdentityForClone();
            return clone;
        }
    }
}
