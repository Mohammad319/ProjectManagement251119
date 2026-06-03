using Domain.Entities.ResourceType;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Mapping.ResourceType
{
    public static class ResourceTypeDtoMapper
    {
        public static ResourceTypeModel ToModel(this ResourceTypeEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new ResourceTypeModel
            {
                Id = entity.Id,
                Name = entity.Name,
                Type = entity.Kind,
                IsVisible = entity.IsVisible,
                IsDefault = entity.IsDefault,
                Order = entity.SortOrder,
                AccountId = entity.AccountId,
                Data = entity.GetMetadataSnapshot()
            };
        }

        public static ResourceSortModel ToModel(this ResourceSortEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new ResourceSortModel
            {
                Id = entity.Id,
                Name = entity.Name,
                IsVisible = entity.IsVisible,
                IsDefault = entity.IsDefault,
                Order = entity.SortOrder,
                ResourceTypeId = entity.ResourceTypeId,
                AccountId = entity.AccountId,
                Data = entity.GetMetadataSnapshot()
            };
        }

        public static ListResourceTypeDTO ToListDto(this ResourceTypeEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new ListResourceTypeDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                Type = entity.Kind,
                Order = entity.SortOrder,
                IsVisible = entity.IsVisible,
                AccountId = entity.AccountId ?? 0,
                Data = entity.GetMetadataSnapshot(),
                ResourcesSort = entity.ResourcesSort
                    .OrderBy(x => x.SortOrder)
                    .Select(ToListDto)
                    .ToList()
            };
        }

        public static ListResourceSortDTO ToListDto(this ResourceSortEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new ListResourceSortDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                Order = entity.SortOrder,
                IsVisible = entity.IsVisible,
                AccountId = entity.AccountId ?? 0,
                Data = entity.GetMetadataSnapshot()
            };
        }
    }
}
