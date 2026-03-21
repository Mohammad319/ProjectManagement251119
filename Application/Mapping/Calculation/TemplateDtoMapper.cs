using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Mapping.Calculation
{
    public static class TemplateDtoMapper
    {
        public static TemplateData ToTemplateData(this TemplateListPostDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            return dto.Data.Clone();
        }

        public static TemplateModelDTO ToModel(this TemplateEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new TemplateModelDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                Data = entity.GetMetadataSnapshot()
            };
        }
    }
}
