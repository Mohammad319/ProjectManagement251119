using Domain.Entities.Calculation;
using ProjectManagement.Shared.Constants;
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

        public static List<NetColumnState> ToNetCalcColumns(this TemplateListPostDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            return TemplateDefaults.EnsureNetCalcColumns(dto.NetCalc?.Columns);
        }

        public static List<NetColumnState> ToColumns(this TemplateColumnPostDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            return TemplateDefaults.EnsureNetCalcColumns(dto.Columns);
        }

        public static TemplateModelDTO ToModel(this TemplateEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new TemplateModelDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                DepartmentId = entity.DepartmentId,
                IsVisible = entity.IsVisible,
                Data = entity.GetMetadataSnapshot()
            };
        }

        public static TemplateColumnModelDTO ToModel(this TemplateColumnEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new TemplateColumnModelDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                DepartmentId = entity.DepartmentId,
                IsVisible = entity.IsVisible,
                Columns = entity.GetColumnsSnapshot()
            };
        }
    }
}
