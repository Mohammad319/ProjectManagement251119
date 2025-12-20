using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Persistence.Serialization;

internal static class EfCoreJsonConversionExtensions
{
    /// <summary>
    /// يضيف Conversion لـ JSON للـ EF Core مع خيارات JsonOptions.Default.
    /// ملاحظة: يجب أن تكون T مرجع (class) حتى نقدر نرجع new() عند null.
    /// </summary>
    public static PropertyBuilder<T> HasJsonConversion<T>(
        this PropertyBuilder<T> propertyBuilder,
        JsonSerializerOptions? options = null)
        where T : class, new()
    {
        var opts = options ?? JsonOptions.Default;

        var converter = new ValueConverter<T, string>(
            v => JsonSerializer.Serialize(v, opts),
            v => JsonSerializer.Deserialize<T>(v, opts) ?? new T());

        return propertyBuilder.HasConversion(converter);
    }
}
