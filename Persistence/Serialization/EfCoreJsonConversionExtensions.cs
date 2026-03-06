using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Persistence.Serialization;

internal static class EfCoreJsonConversionExtensions
{
    public static PropertyBuilder<T> HasJsonConversion<T>(
        this PropertyBuilder<T> propertyBuilder,
        JsonSerializerOptions? options = null)
        where T : class, new()
    {
        var opts = options ?? JsonOptions.Default;

        var converter = new ValueConverter<T, string>(
            v => JsonSerializer.Serialize(v ?? new T(), opts),
            v => string.IsNullOrWhiteSpace(v)
                ? new T()
                : (JsonSerializer.Deserialize<T>(v, opts) ?? new T()));

        // Important: lets EF detect changes for mutable JSON objects (reference types)
        var comparer = new ValueComparer<T>(
            (l, r) =>
                (l == null && r == null)
                || (l != null && r != null
                    && JsonSerializer.Serialize(l, opts) == JsonSerializer.Serialize(r, opts)),

            v => v == null ? 0 : JsonSerializer.Serialize(v, opts).GetHashCode(),

            v => v == null
                ? new T()
                : (JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, opts), opts) ?? new T())
        );

        propertyBuilder.HasConversion(converter);
        propertyBuilder.Metadata.SetValueComparer(comparer);

        return propertyBuilder;
    }
}