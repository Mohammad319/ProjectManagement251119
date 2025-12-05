using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace ProjectImportHub.Infrastructure.Extensions;

public static class PropertyBuilderJsonExtensions
{
    public static PropertyBuilder<T> HasJsonConversionWithComparer<T>(this PropertyBuilder<T> propertyBuilder)
        where T : class, new()
    {
        propertyBuilder.HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<T>(v, JsonSerializerOptions.Default) ?? new T()
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<T>(
                (a, b) =>
                    JsonSerializer.Serialize(a, JsonSerializerOptions.Default)
                    == JsonSerializer.Serialize(b, JsonSerializerOptions.Default),

                v =>
                    JsonSerializer.Serialize(v, JsonSerializerOptions.Default).GetHashCode(),

                v =>
                    JsonSerializer.Deserialize<T>(
                        JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                        JsonSerializerOptions.Default
                    ) ?? new T()
            )
        );

        return propertyBuilder;
    }
    public static PropertyBuilder<T?> HasJsonNullableConversionWithComparer<T>(
    this PropertyBuilder<T?> propertyBuilder)
    where T : class, new()
    {
        propertyBuilder.HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<T>(v, JsonSerializerOptions.Default) ?? new T()
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<T?>(
                (a, b) =>
                    JsonSerializer.Serialize(a, JsonSerializerOptions.Default)
                    == JsonSerializer.Serialize(b, JsonSerializerOptions.Default),

                v =>
                    JsonSerializer.Serialize(v, JsonSerializerOptions.Default).GetHashCode(),

                v =>
                    JsonSerializer.Deserialize<T>(
                        JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                        JsonSerializerOptions.Default
                    ) ?? new T()
            )
        );

        return propertyBuilder;
    }

    // للخصائص غير nullable: List<T>
    public static PropertyBuilder<List<T>> HasJsonListComparer<T>(this PropertyBuilder<List<T>> propertyBuilder)
        where T : class
    {
        propertyBuilder.HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<List<T>>(v, JsonSerializerOptions.Default) ?? new List<T>()
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<List<T>>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),

                v => v == null
                    ? 0
                    : v.Aggregate(
                        0,
                        (hash, item) => HashCode.Combine(
                            hash,
                            item == null ? 0 : item.GetHashCode()
                        )
                    ),

                v => v == null ? new List<T>() : v.ToList()
            )
        );

        return propertyBuilder;
    }

    // للخصائص nullable: List<T>?
    public static PropertyBuilder<List<T>?> HasJsonNullableListComparer<T>(this PropertyBuilder<List<T>?> propertyBuilder)
        where T : class
    {
        propertyBuilder.HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<List<T>>(v, JsonSerializerOptions.Default) ?? new List<T>()
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<List<T>?>(
                (a, b) =>
                    a != null && b != null && a.SequenceEqual(b),

                v => v == null
                    ? 0
                    : v.Aggregate(
                        0,
                        (hash, item) => HashCode.Combine(
                            hash,
                            item == null ? 0 : item.GetHashCode()
                        )
                    ),

                v => v == null ? new List<T>() : v.ToList()
            )
        );

        return propertyBuilder;
    }
}
