using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Shared.Helper
{
    public static class CalculationCloneHelper
    {
        public static List<QuanityListDTO> CloneQuantities(IEnumerable<QuanityListDTO>? values)
            => values?.Select(CloneQuantity).ToList() ?? [];

        public static QuanityListDTO CloneQuantity(QuanityListDTO? value)
            => new()
            {
                Name = MetadataCloneHelper.CopyText(value?.Name),
                Quantity = value?.Quantity
            };

        public static List<IncomeBase> CloneIncome(IEnumerable<IncomeBase>? values)
            => values?.Select(CloneIncomeItem).ToList() ?? [];

        public static IncomeBase CloneIncomeItem(IncomeBase? value)
            => new()
            {
                Year = value?.Year ?? 0,
                Description = MetadataCloneHelper.CopyText(value?.Description),
                SubType = value?.SubType ?? default,
                Q1 = value?.Q1 ?? 0m,
                Q2 = value?.Q2 ?? 0m,
                Q3 = value?.Q3 ?? 0m,
                Q4 = value?.Q4 ?? 0m
            };

        public static List<OHFactors> CloneFactors(IEnumerable<OHFactors>? values)
            => values?.Select(CloneFactor).ToList() ?? [];

        public static OHFactors CloneFactor(OHFactors? value)
            => new()
            {
                ResourceType = value?.ResourceType ?? default,
                SortId = value?.SortId,
                ResId = value?.ResId,
                IsLocked = value?.IsLocked ?? true,
                Earnings = value?.Earnings ?? 20,
                Key = value?.Key ?? 0,
                Unit = MetadataCloneHelper.CopyText(value?.Unit),
                DivisionKey = value?.DivisionKey ?? 0,
                Selected = string.IsNullOrWhiteSpace(value?.Selected) ? "all" : value.Selected
            };

        public static List<HourlyPriceListGroupDTO> CloneHourlyPrice(IEnumerable<HourlyPriceListGroupDTO>? values)
            => values?.Select(CloneHourlyPriceGroup).ToList() ?? [];

        public static HourlyPriceListGroupDTO CloneHourlyPriceGroup(HourlyPriceListGroupDTO? value)
            => new()
            {
                Code = MetadataCloneHelper.CopyText(value?.Code),
                Name = MetadataCloneHelper.CopyText(value?.Name),
                Comment = MetadataCloneHelper.CopyText(value?.Comment),
                SubItemsVisible = value?.SubItemsVisible ?? true,
                Items = value?.Items?.Select(CloneHourlyPriceItem).ToList() ?? []
            };

        public static HourlyPriceListItemDTO CloneHourlyPriceItem(HourlyPriceListItemDTO? value)
            => new()
            {
                Code = MetadataCloneHelper.CopyText(value?.Code),
                Name = MetadataCloneHelper.CopyText(value?.Name),
                Unit = MetadataCloneHelper.CopyText(value?.Unit),
                Quantity = value?.Quantity ?? 0,
                CostMarketPrices = value?.CostMarketPrices ?? 0,
                CostSubmittedPrices = value?.CostSubmittedPrices ?? 0,
                Comment = MetadataCloneHelper.CopyText(value?.Comment)
            };
    }
}
