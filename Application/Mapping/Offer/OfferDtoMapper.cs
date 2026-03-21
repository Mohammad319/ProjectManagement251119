using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Offer;
using System.Linq.Expressions;

namespace Application.Mapping.Offer
{
    public static class OfferDtoMapper
    {
        public static OfferData ToData(this PostOfferDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var data = dto.Data.Clone();
            data.Comment = dto.Comment ?? string.Empty;
            data.Normalize();
            return data;
        }

        public static ListOfferDTO ToListDto(this OfferEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var metadata = entity.GetMetadataSnapshot();

            return new ListOfferDTO
            {
                Id = entity.Id,
                RowVersion = entity.RowVersion,
                BaseCost = metadata.BaseCost,
                Cost = metadata.Cost,
                Contact = metadata.Contact,
                Comment = entity.Comment ?? string.Empty,
                Date = entity.Date,
                OrganisationId = entity.OrganisationId,
                Organisation = entity.Organisation?.Name ?? string.Empty,
                SubCategory = entity.Organisation?.OrganisationCategory?.Name ?? string.Empty,
                Category = entity.Organisation?.OrganisationCategory?.ParentCategory?.Name ?? string.Empty
            };
        }

        public static Expression<Func<OfferEntity, ListOfferDTO>> ProjectListDto()
            => x => new ListOfferDTO
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BaseCost = x.Metadata.BaseCost,
                Cost = x.Metadata.Cost,
                Contact = x.Metadata.Contact,
                Organisation = x.Organisation != null ? x.Organisation.Name : string.Empty,
                Comment = x.Comment ?? string.Empty,
                Date = x.Date,
                OrganisationId = x.OrganisationId,
                SubCategory = x.Organisation != null && x.Organisation.OrganisationCategory != null
                    ? x.Organisation.OrganisationCategory.Name
                    : string.Empty,
                Category = x.Organisation != null && x.Organisation.OrganisationCategory != null && x.Organisation.OrganisationCategory.ParentCategory != null
                    ? x.Organisation.OrganisationCategory.ParentCategory.Name
                    : string.Empty
            };
    }
}
