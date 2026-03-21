using Domain.Entities.Organisation;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Mapping.Organisation
{
    public static class OrganisationDtoMapper
    {
        public static PostOrganisationDTO ToPostDto(this OrganisationEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new PostOrganisationDTO
            {
                Name = entity.Name,
                CategoryId = entity.OrganisationCategoryId,
                OrganisationTypeID = entity.OrganisationTypeId,
                IsVisible = entity.IsVisible,
                Data = entity.GetMetadataSnapshot()
            };
        }

        public static OrganisationDetailsDTO ToDetailsDto(this OrganisationEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new OrganisationDetailsDTO
            {
                Name = entity.Name,
                Category = entity.OrganisationCategory?.ParentCategory?.Name ?? string.Empty,
                SubCategory = entity.OrganisationCategory?.Name ?? string.Empty,
                Type = entity.OrganisationType?.Name ?? string.Empty,
                Data = entity.GetMetadataSnapshot()
            };
        }
    }
}
