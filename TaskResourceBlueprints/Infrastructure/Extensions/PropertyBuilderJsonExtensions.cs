using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace TaskResourceBlueprints.Infrastructure.Extensions;

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
                (a, b) => Serialize(a) == Serialize(b),
                v => Serialize(v).GetHashCode(),
                v => DeepClone(v)
            )
        );

        return propertyBuilder;
    }

    public static PropertyBuilder<T?> HasJsonNullableConversionWithComparer<T>(this PropertyBuilder<T?> propertyBuilder)
        where T : class, new()
    {
        propertyBuilder.HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<T>(v, JsonSerializerOptions.Default)
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<T?>(
                (a, b) => Serialize(a) == Serialize(b),
                v => Serialize(v).GetHashCode(),
                v => v == null ? null : DeepClone(v)
            )
        );

        return propertyBuilder;
    }

    public static PropertyBuilder<List<T>> HasJsonListComparer<T>(this PropertyBuilder<List<T>> propertyBuilder)
        where T : class
    {
        propertyBuilder.HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<List<T>>(v, JsonSerializerOptions.Default) ?? new List<T>()
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<List<T>>(
                (a, b) => SequenceEqual(a, b),
                v => GetListHashCode(v),
                v => v == null ? new List<T>() : v.ToList()
            )
        );

        return propertyBuilder;
    }

    public static PropertyBuilder<List<T>?> HasJsonNullableListComparer<T>(this PropertyBuilder<List<T>?> propertyBuilder)
        where T : class
    {
        propertyBuilder.HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<List<T>>(v, JsonSerializerOptions.Default)
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<List<T>?>(
                (a, b) => SequenceEqual(a, b),
                v => GetListHashCode(v),
                v => v == null ? null : v.ToList()
            )
        );

        return propertyBuilder;
    }

    public static PropertyBuilder<List<T>> HasJsonScalarListComparer<T>(this PropertyBuilder<List<T>> propertyBuilder)
        where T : struct
    {
        propertyBuilder.HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<List<T>>(v, JsonSerializerOptions.Default) ?? new List<T>()
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<List<T>>(
                (a, b) => SequenceEqual(a, b),
                v => GetListHashCode(v),
                v => v == null ? new List<T>() : v.ToList()
            )
        );

        return propertyBuilder;
    }

    public static PropertyBuilder<List<T>?> HasJsonNullableScalarListComparer<T>(this PropertyBuilder<List<T>?> propertyBuilder)
        where T : struct
    {
        propertyBuilder.HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<List<T>>(v, JsonSerializerOptions.Default)
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<List<T>?>(
                (a, b) => SequenceEqual(a, b),
                v => GetListHashCode(v),
                v => v == null ? null : v.ToList()
            )
        );

        return propertyBuilder;
    }

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonSerializerOptions.Default);

    private static T DeepClone<T>(T value) =>
        JsonSerializer.Deserialize<T>(Serialize(value), JsonSerializerOptions.Default)!;

    private static bool SequenceEqual<T>(IReadOnlyCollection<T>? a, IReadOnlyCollection<T>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        return a.SequenceEqual(b);
    }

    private static int GetListHashCode<T>(IEnumerable<T>? items)
    {
        if (items == null) return 0;

        var hash = new HashCode();
        foreach (var item in items)
            hash.Add(item);

        return hash.ToHashCode();
    }
}