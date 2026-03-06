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

        // IMPORTANT: Needed for mutable reference types (JSON) so EF can detect changes
        var comparer = new ValueComparer<T>(
            (l, r) =>
            {
                if (ReferenceEquals(l, r)) return true;
                if (l is null && r is null) return true;
                return JsonSerializer.Serialize(l ?? new T(), opts)
                     == JsonSerializer.Serialize(r ?? new T(), opts);
            },
            v => JsonSerializer.Serialize(v ?? new T(), opts).GetHashCode(),
            v => JsonSerializer.Deserialize<T>(
                    JsonSerializer.Serialize(v ?? new T(), opts), opts
                ) ?? new T()
        );

        propertyBuilder.HasConversion(converter);
        propertyBuilder.Metadata.SetValueComparer(comparer);

        return propertyBuilder;
    }
}