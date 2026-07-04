using Domain.Entities.Application;
using ProjectManagement.Shared.Base.Application;
using ProjectManagement.Shared.DTO.App;

namespace Application.Mapping.App
{
    public static class ApplicationDtoMapper
    {
        public static ApplicationDTO ToDto(this ApplicationEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new ApplicationDTO
            {
                Id = entity.Id,
                DepartmentId = entity.DepartmentId,
                Name = entity.Name ?? string.Empty,
                IsVisible = entity.IsVisible,
                UserId = entity.UserId,
                LastUpdate = entity.LastUpdate,
                Data = entity.Data.ToDto()
            };
        }

        public static ApplicationValuesDTO ToDto(this ApplicationValuesEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new ApplicationValuesDTO
            {
                Id = entity.Id,
                CalculationId = entity.CalculationId,
                ApplicationId = entity.ApplicationId,
                UserId = entity.UserId,
                Name = entity.Name ?? string.Empty,
                Responsible = entity.Responsible ?? string.Empty,
                LastUpdate = entity.LastUpdate,
                Data = entity.Data?.Clone() ?? new ApplicationValuesData(),
                Application = entity.Application is null ? null : entity.Application.ToDto()
            };
        }

        public static ApplicationEntity ToEntity(this ApplicationDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ApplicationEntity
            {
                Id = dto.Id,
                DepartmentId = dto.DepartmentId,
                Name = dto.Name ?? string.Empty,
                IsVisible = dto.IsVisible,
                UserId = dto.UserId,
                LastUpdate = dto.LastUpdate,
                Data = dto.Data.ToEntity()
            };
        }

        public static ApplicationValuesEntity ToEntity(this ApplicationValuesDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ApplicationValuesEntity
            {
                Id = dto.Id,
                CalculationId = dto.CalculationId,
                ApplicationId = dto.ApplicationId,
                UserId = dto.UserId,
                Name = dto.Name ?? string.Empty,
                Responsible = dto.Responsible ?? string.Empty,
                LastUpdate = dto.LastUpdate,
                Data = dto.Data?.Clone() ?? new ApplicationValuesData()
            };
        }

        private static ApplicationDataDTO ToDto(this ApplicationDataEntity? entity)
        {
            entity ??= new ApplicationDataEntity();

            return new ApplicationDataDTO
            {
                Description = entity.Description ?? string.Empty,
                TemplateType = entity.TemplateType ?? SelfInspectionTemplateTypes.Checklist,
                Purpose = entity.Purpose ?? string.Empty,
                IsSystemTemplate = entity.IsSystemTemplate,
                SystemTemplateKey = entity.SystemTemplateKey ?? string.Empty,
                CopiedFromSystemTemplateKey = entity.CopiedFromSystemTemplateKey ?? string.Empty,
                Rows = entity.Rows?.Select(ToDto).ToList() ?? []
            };
        }

        private static ApplicationDataEntity ToEntity(this ApplicationDataDTO? dto)
        {
            dto ??= new ApplicationDataDTO();

            return new ApplicationDataEntity
            {
                Description = dto.Description ?? string.Empty,
                TemplateType = dto.TemplateType ?? SelfInspectionTemplateTypes.Checklist,
                Purpose = dto.Purpose ?? string.Empty,
                IsSystemTemplate = dto.IsSystemTemplate,
                SystemTemplateKey = dto.SystemTemplateKey ?? string.Empty,
                CopiedFromSystemTemplateKey = dto.CopiedFromSystemTemplateKey ?? string.Empty,
                Rows = dto.Rows?.Select(ToEntity).ToList() ?? []
            };
        }

        private static RowDTO ToDto(this RowEntity entity)
        {
            return new RowDTO
            {
                ID = entity.ID,
                Name = entity.Name ?? string.Empty,
                Description = entity.Description ?? string.Empty,
                Style = entity.Style ?? string.Empty,
                StyleRow = entity.StyleRow ?? string.Empty,
                IsVisible = entity.IsVisible,
                Attributes = entity.Attributes?.Select(ToDto).ToList() ?? []
            };
        }

        private static RowEntity ToEntity(this RowDTO dto)
        {
            return new RowEntity
            {
                ID = dto.ID,
                Name = dto.Name ?? string.Empty,
                Description = dto.Description ?? string.Empty,
                Style = dto.Style ?? string.Empty,
                StyleRow = dto.StyleRow ?? string.Empty,
                IsVisible = dto.IsVisible,
                Attributes = dto.Attributes?.Select(ToBase).ToList() ?? []
            };
        }

        private static AttributeDTO ToDto(this AttributeBase entity)
        {
            return new AttributeDTO
            {
                ID = entity.ID,
                AttributeType = entity.AttributeType,
                Required = entity.Required,
                Order = entity.Order,
                Validation = entity.Validation ?? string.Empty,
                Style = entity.Style ?? string.Empty,
                Value = string.Empty
            };
        }

        private static AttributeBase ToBase(this AttributeDTO dto)
        {
            return new AttributeBase
            {
                ID = dto.ID,
                AttributeType = dto.AttributeType,
                Required = dto.Required,
                Order = dto.Order,
                Validation = dto.Validation ?? string.Empty,
                Style = dto.Style ?? string.Empty
            };
        }
    }
}
