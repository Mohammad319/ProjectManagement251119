using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Client.Shared.ViewModel
{
    public sealed class CalculationFilterPresetStore
    {
        public string? ActivePresetId { get; set; }
        public List<CalculationFilterPreset> Presets { get; set; } = [];
    }

    public sealed class CalculationFilterPreset
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public FilterVM Filter { get; set; } = new();
        public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public static class CalculationFilterPresetState
    {
        public static string GetStorageKey(int calculationId) => $"calc-table-filters:{calculationId}";

        public static string GetLegacyStorageKey(int calculationId) => $"calc-table-filter:{calculationId}";

        public static CalculationFilterPresetStore NormalizeStore(CalculationFilterPresetStore? store)
        {
            store ??= new CalculationFilterPresetStore();
            store.Presets ??= [];

            for (int i = store.Presets.Count - 1; i >= 0; i--)
            {
                var preset = store.Presets[i];
                if (preset is null)
                {
                    store.Presets.RemoveAt(i);
                    continue;
                }

                preset.Id = string.IsNullOrWhiteSpace(preset.Id)
                    ? Guid.NewGuid().ToString("N")
                    : preset.Id;
                preset.Name = string.IsNullOrWhiteSpace(preset.Name)
                    ? $"Filter {i + 1}"
                    : preset.Name.Trim();
                preset.Filter = NormalizeFilter(preset.Filter);

                if (preset.SavedAtUtc == default)
                    preset.SavedAtUtc = DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(store.ActivePresetId) &&
                store.Presets.All(x => !string.Equals(x.Id, store.ActivePresetId, StringComparison.Ordinal)))
            {
                store.ActivePresetId = null;
            }

            return store;
        }

        public static CalculationFilterPreset? GetPreset(CalculationFilterPresetStore? store, string? presetId)
        {
            if (store is null || string.IsNullOrWhiteSpace(presetId))
                return null;

            return store.Presets.FirstOrDefault(x => string.Equals(x.Id, presetId, StringComparison.Ordinal));
        }

        public static CalculationFilterPreset? GetActivePreset(CalculationFilterPresetStore? store)
        {
            store = NormalizeStore(store);
            return GetPreset(store, store.ActivePresetId);
        }

        public static void SetActivePreset(CalculationFilterPresetStore store, string? presetId)
        {
            NormalizeStore(store);
            store.ActivePresetId = GetPreset(store, presetId)?.Id;
        }

        public static CalculationFilterPreset SavePreset(
            CalculationFilterPresetStore store,
            string name,
            FilterVM filter,
            string? presetId = null)
        {
            NormalizeStore(store);

            var trimmedName = string.IsNullOrWhiteSpace(name)
                ? $"Filter {store.Presets.Count + 1}"
                : name.Trim();

            var preset = GetPreset(store, presetId)
                ?? store.Presets.FirstOrDefault(x => string.Equals(x.Name, trimmedName, StringComparison.OrdinalIgnoreCase));

            if (preset is not null)
                store.Presets.Remove(preset);

            preset ??= new CalculationFilterPreset();
            preset.Name = trimmedName;
            preset.Filter = CloneFilter(filter);
            preset.SavedAtUtc = DateTime.UtcNow;

            store.Presets.Insert(0, preset);
            store.ActivePresetId = preset.Id;
            return preset;
        }

        public static bool RemovePreset(CalculationFilterPresetStore store, string? presetId)
        {
            NormalizeStore(store);

            var preset = GetPreset(store, presetId);
            if (preset is null)
                return false;

            store.Presets.Remove(preset);

            if (string.Equals(store.ActivePresetId, preset.Id, StringComparison.Ordinal))
                store.ActivePresetId = null;

            return true;
        }

        public static FilterVM CloneFilter(FilterVM source) => new()
        {
            FilterVisible = source.FilterVisible,
            Code = [.. source.Code],
            CodeFilterType = source.CodeFilterType,
            Name = [.. source.Name],
            NameFilterType = source.NameFilterType,
            Account = [.. source.Account],
            AccountFilterType = source.AccountFilterType,
            Status = [.. source.Status],
            StatusFilterType = source.StatusFilterType,
            ResourceTypeId = source.ResourceTypeId,
            Resource = [.. source.Resource],
            ResourceFilterType = source.ResourceFilterType,
            ResourceTypeSystem = [.. source.ResourceTypeSystem],
            ResourceTypeFilterTypeSystem = source.ResourceTypeFilterTypeSystem,
            ResourceSortId = source.ResourceSortId,
            ResourceSort = [.. source.ResourceSort],
            ResourceSortFilterType = source.ResourceSortFilterType,
            Unit = [.. source.Unit],
            UnitFilterType = source.UnitFilterType
        };

        public static FilterVM NormalizeFilter(FilterVM? filter)
        {
            filter ??= new FilterVM();
            filter.Code ??= [];
            filter.Name ??= [];
            filter.Account ??= [];
            filter.Status ??= [];
            filter.Resource ??= [];
            filter.ResourceTypeSystem ??= [];
            filter.ResourceSort ??= [];
            filter.Unit ??= [];
            return filter;
        }
    }
}
