using System.Text.Json;

namespace Persistence.Serialization;

internal static class JsonOptions
{
    // خيار موحد لتقليل الـ allocations وتوحيد السلوك عبر المشروع
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
