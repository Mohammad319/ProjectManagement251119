using System.Text.Json;

namespace TaskResourceBlueprints.Infrastructure;

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
