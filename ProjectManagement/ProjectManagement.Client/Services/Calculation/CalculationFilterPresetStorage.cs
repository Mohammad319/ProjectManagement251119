using System;
using System.Text.Json;
using Microsoft.JSInterop;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.ViewModel;

namespace ProjectManagement.Client.Services.Calculation;

public sealed class CalculationFilterPresetStorage(
    IJSRuntime js,
    IClientLogger clientLogger)
{
    public async Task<CalculationFilterPresetStore> LoadAsync(int calculationId)
    {
        try
        {
            var json = await js.InvokeAsync<string?>(
                "localStorage.getItem",
                CalculationFilterPresetState.GetStorageKey(calculationId));

            if (!string.IsNullOrWhiteSpace(json))
            {
                var store = JsonSerializer.Deserialize<CalculationFilterPresetStore>(json);
                return CalculationFilterPresetState.NormalizeStore(store);
            }

            var legacyJson = await js.InvokeAsync<string?>(
                "localStorage.getItem",
                CalculationFilterPresetState.GetLegacyStorageKey(calculationId));

            if (string.IsNullOrWhiteSpace(legacyJson))
                return new CalculationFilterPresetStore();

            var legacyFilter = JsonSerializer.Deserialize<FilterVM>(legacyJson);
            if (legacyFilter is null)
                return new CalculationFilterPresetStore();

            var migratedStore = new CalculationFilterPresetStore();
            CalculationFilterPresetState.SavePreset(migratedStore, "Saved filter", legacyFilter);
            await SaveAsync(calculationId, migratedStore);

            await js.InvokeVoidAsync(
                "localStorage.removeItem",
                CalculationFilterPresetState.GetLegacyStorageKey(calculationId));

            return migratedStore;
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync("Loading calculation filter presets failed", ex: ex);
            return new CalculationFilterPresetStore();
        }
    }

    public async Task SaveAsync(int calculationId, CalculationFilterPresetStore store)
    {
        try
        {
            var normalizedStore = CalculationFilterPresetState.NormalizeStore(store);
            var json = JsonSerializer.Serialize(normalizedStore);

            await js.InvokeVoidAsync(
                "localStorage.setItem",
                CalculationFilterPresetState.GetStorageKey(calculationId),
                json);
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync("Saving calculation filter presets failed", ex: ex);
        }
    }
}
