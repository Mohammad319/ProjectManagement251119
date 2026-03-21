using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Mapping.Calculation
{
    public static class OpportunityDtoMapper
    {
        public static OpportunityData ToMetadata(this PostOpportunityDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            return dto.Data.Clone();
        }

        public static OpportunityListDTO ToListDto(this OpportunityEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new OpportunityListDTO
            {
                Id = entity.Id,
                CalculationId = entity.CalculationId,
                OpportunitiesRisks = entity.OpportunitiesRisks,
                OpportunityType = entity.OpportunityType ?? string.Empty,
                Metadata = entity.GetMetadataSnapshot()
            };
        }
    }
}
