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

            var snapshot = entity.GetMetadataSnapshot();

            return new OrganisationDetailsDTO
            {
                Name = entity.Name,
                Category = entity.OrganisationCategory?.ParentCategory?.Name ?? string.Empty,
                SubCategory = entity.OrganisationCategory?.Name ?? string.Empty,
                // Prefer the fixed catalog value stored in metadata; fall back to the legacy
                // OrganisationType lookup name for records created before the switch.
                Type = !string.IsNullOrWhiteSpace(snapshot.OrganisationType)
                    ? snapshot.OrganisationType
                    : entity.OrganisationType?.Name ?? string.Empty,
                Data = snapshot
            };
        }
    }
}
