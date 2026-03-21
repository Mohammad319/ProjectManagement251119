using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;
using System.Linq.Expressions;

namespace Application.Mapping.Calculation
{
    public static class ShareCalcDtoMapper
    {
        public static ShareCalcData ToMetadata(this ShareCalcTabsDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ShareCalcData
            {
                Tabs = dto.Tabs
            };
        }

        public static ShareCalcUpsertDTO ToUpsert(this PostShareCalcDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ShareCalcUpsertDTO
            {
                Id = null,
                CalculationId = dto.CalculationId,
                DepartmentId = dto.DepartmentId,
                Tabs = dto.Tabs
            };
        }

        public static ShareCalcUpsertDTO ToUpsert(this UpdateShareCalcDTO dto, int departmentId)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ShareCalcUpsertDTO
            {
                Id = dto.Id,
                DepartmentId = departmentId,
                Tabs = dto.Tabs
            };
        }

        public static Expression<Func<ShareCalcEntity, ListShareCalcDTO>> ProjectListDto()
            => x => new ListShareCalcDTO
            {
                Id = x.Id,
                DepartmentId = x.DepartmentId,
                UserId = x.CreatedBy,
                User = ((x.CreatedAtUser == null ? string.Empty : (x.CreatedAtUser.FirstName ?? string.Empty)) + " " +
                        (x.CreatedAtUser == null ? string.Empty : (x.CreatedAtUser.LastName ?? string.Empty))).Trim(),
                Tabs = x.Metadata.Tabs,
                Department = x.Department == null ? string.Empty : (x.Department.Name ?? string.Empty)
            };
    }
}
