using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public sealed class DisplayOptionsPreset
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public bool ShowTasks { get; set; } = true;
        public bool ShowResources { get; set; } = true;
        public bool ShowComments { get; set; } = true;
        public bool ShowResourceVariables { get; set; } = true;
        public bool OnlyActive { get; set; } = false;
        public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;

        public List<int> InactiveTaskIds { get; set; } = [];
        public List<int> InactiveResourceIds { get; set; } = [];

        public bool IsTaskActive(int taskId) => !InactiveTaskIds.Contains(taskId);
        public bool IsResourceActive(int resourceId) => !InactiveResourceIds.Contains(resourceId);

        public void ToggleTask(int taskId)
        {
            if (!InactiveTaskIds.Remove(taskId)) InactiveTaskIds.Add(taskId);
        }

        public void ToggleResource(int resourceId)
        {
            if (!InactiveResourceIds.Remove(resourceId)) InactiveResourceIds.Add(resourceId);
        }

        public DisplayOptionsPreset Clone() => new()
        {
            Id = Id,
            Name = Name,
            ShowTasks = ShowTasks,
            ShowResources = ShowResources,
            ShowComments = ShowComments,
            ShowResourceVariables = ShowResourceVariables,
            OnlyActive = OnlyActive,
            SavedAtUtc = SavedAtUtc,
            InactiveTaskIds = [.. InactiveTaskIds],
            InactiveResourceIds = [.. InactiveResourceIds],
        };
    }

    public sealed class DisplayOptionsPresetStore
    {
        public string? ActivePresetId { get; set; }
        public List<DisplayOptionsPreset> Presets { get; set; } = [];
    }

    public static class DisplayOptionsPresetState
    {
        public static DisplayOptionsPresetStore Normalize(DisplayOptionsPresetStore? store)
        {
            store ??= new DisplayOptionsPresetStore();
            store.Presets ??= [];

            for (int i = store.Presets.Count - 1; i >= 0; i--)
            {
                var p = store.Presets[i];
                if (p is null) { store.Presets.RemoveAt(i); continue; }
                if (string.IsNullOrWhiteSpace(p.Id)) p.Id = Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(p.Name)) p.Name = $"Preset {i + 1}";
                else p.Name = p.Name.Trim();
                if (p.SavedAtUtc == default) p.SavedAtUtc = DateTime.UtcNow;
                p.InactiveTaskIds ??= [];
                p.InactiveResourceIds ??= [];
            }

            if (!string.IsNullOrWhiteSpace(store.ActivePresetId) &&
                store.Presets.All(x => !string.Equals(x.Id, store.ActivePresetId, StringComparison.Ordinal)))
                store.ActivePresetId = null;

            return store;
        }

        public static DisplayOptionsPreset? GetPreset(DisplayOptionsPresetStore? store, string? id)
        {
            if (store is null || string.IsNullOrWhiteSpace(id)) return null;
            return store.Presets.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
        }

        public static DisplayOptionsPreset? GetActivePreset(DisplayOptionsPresetStore? store)
        {
            store = Normalize(store);
            return GetPreset(store, store.ActivePresetId);
        }

        public static void SetActivePreset(DisplayOptionsPresetStore store, string? id)
        {
            Normalize(store);
            store.ActivePresetId = GetPreset(store, id)?.Id;
        }

        public static DisplayOptionsPreset SavePreset(
            DisplayOptionsPresetStore store,
            string name,
            DisplayOptionsPreset data,
            string? targetId = null)
        {
            Normalize(store);

            var trimmedName = string.IsNullOrWhiteSpace(name)
                ? $"Preset {store.Presets.Count + 1}"
                : name.Trim();

            var preset = GetPreset(store, targetId)
                ?? store.Presets.FirstOrDefault(x => string.Equals(x.Name, trimmedName, StringComparison.OrdinalIgnoreCase));

            if (preset is not null) store.Presets.Remove(preset);

            preset ??= new DisplayOptionsPreset();
            preset.Name = trimmedName;
            preset.ShowTasks = data.ShowTasks;
            preset.ShowResources = data.ShowResources;
            preset.ShowComments = data.ShowComments;
            preset.ShowResourceVariables = data.ShowResourceVariables;
            preset.OnlyActive = data.OnlyActive;
            preset.InactiveTaskIds = data.InactiveTaskIds is null ? [] : [.. data.InactiveTaskIds];
            preset.InactiveResourceIds = data.InactiveResourceIds is null ? [] : [.. data.InactiveResourceIds];
            preset.SavedAtUtc = DateTime.UtcNow;

            store.Presets.Insert(0, preset);
            store.ActivePresetId = preset.Id;
            return preset;
        }

        public static bool RemovePreset(DisplayOptionsPresetStore store, string? id)
        {
            Normalize(store);
            var preset = GetPreset(store, id);
            if (preset is null) return false;
            store.Presets.Remove(preset);
            if (string.Equals(store.ActivePresetId, preset.Id, StringComparison.Ordinal))
                store.ActivePresetId = null;
            return true;
        }
    }
}
