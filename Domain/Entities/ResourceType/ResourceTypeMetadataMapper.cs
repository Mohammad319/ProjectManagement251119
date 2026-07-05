using System.Linq;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Domain.Entities.ResourceType
{
    internal static class ResourceTypeMetadataMapper
    {
        public static ResourceTypeData Build(ResourceTypeData? metadata)
        {
            metadata ??= new ResourceTypeData();

            var copy = new ResourceTypeData
            {
                Cost = metadata.Cost,
                Unit = NormalizeOptional(metadata.Unit) ?? string.Empty,
                FixedQ = metadata.FixedQ,
                ChangeFactor1 = metadata.ChangeFactor1,
                ChangeFactor2 = metadata.ChangeFactor2,
                BaseCost = metadata.BaseCost,
                CapWaste = metadata.CapWaste,
                CO2 = metadata.CO2,
                AllowedAccountIds = metadata.AllowedAccountIds is { Count: > 0 }
                    ? metadata.AllowedAccountIds.Where(id => id > 0).Distinct().ToList()
                    : [],
                UseOwnAccountSettings = metadata.UseOwnAccountSettings
            };

            copy.Normalize();
            return copy;
        }

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
