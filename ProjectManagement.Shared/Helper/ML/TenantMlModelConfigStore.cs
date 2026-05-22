using System.Diagnostics;
using System.Text.Json;

namespace ProjectManagement.Shared.Helper.ML;

public sealed class TenantMlModelConfig
{
    public int TenantId { get; set; }
    public TenantMlUsageMode UsageMode { get; set; } = TenantMlUsageMode.TenantWithGlobalFallback;
    public bool IncludeFeedbackInTraining { get; set; } = true;
    public bool AutoTrainingEnabled { get; set; }
    public int AutoTrainingIntervalDays { get; set; } = 14;
    public DateTime? NextTrainingAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class TenantMlModelConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string GetConfigPath(int tenantId)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId));

        var modelPath = TaskResourceSuggestionMlModelPath.GetTenantModelPath(tenantId);
        return Path.Combine(Path.GetDirectoryName(modelPath) ?? AppContext.BaseDirectory, "tenant-ml-settings.json");
    }

    public static TenantMlModelConfig Read(int tenantId)
    {
        var path = GetConfigPath(tenantId);
        if (!File.Exists(path))
            return new TenantMlModelConfig { TenantId = tenantId };

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<TenantMlModelConfig>(json, JsonOptions);
            return config is null || config.TenantId != tenantId
                ? new TenantMlModelConfig { TenantId = tenantId }
                : config;
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            Trace.TraceWarning("Failed to read ML config for tenant {0}: {1}", tenantId, ex.Message);
            return new TenantMlModelConfig { TenantId = tenantId };
        }
    }

    public static TenantMlUsageMode ReadUsageMode(int tenantId)
        => Read(tenantId).UsageMode;

    public static void Write(TenantMlModelConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.TenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.TenantId));

        config.UpdatedAtUtc = DateTime.UtcNow;
        var path = GetConfigPath(config.TenantId);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, JsonSerializer.Serialize(config, JsonOptions));
    }
}
