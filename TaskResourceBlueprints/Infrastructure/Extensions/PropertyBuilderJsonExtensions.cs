using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Infrastructure.Extensions;

public static class PropertyBuilderJsonExtensions
{
    public static PropertyBuilder<T> HasJsonConversionWithComparer<T>(this PropertyBuilder<T> propertyBuilder)
        where T : class, new()
    {
        propertyBuilder.HasConversion(
            v => System.Text.Json.JsonSerializer.Serialize(v, JsonOptions.Default),
            v => System.Text.Json.JsonSerializer.Deserialize<T>(v, JsonOptions.Default) ?? new T()
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<T>(
                (a, b) => ReferenceEquals(a, b) || (a != null && b != null && Serialize(a) == Serialize(b)),
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
            v => System.Text.Json.JsonSerializer.Serialize(v, JsonOptions.Default),
            v => System.Text.Json.JsonSerializer.Deserialize<T>(v, JsonOptions.Default)
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<T?>(
                (a, b) => ReferenceEquals(a, b) || (a == null && b == null) || (a != null && b != null && Serialize(a) == Serialize(b)),
                v => v == null ? 0 : Serialize(v).GetHashCode(),
                v => v == null ? null : DeepClone(v)
            )
        );

        return propertyBuilder;
    }

    public static PropertyBuilder<List<T>> HasJsonListComparer<T>(this PropertyBuilder<List<T>> propertyBuilder)
        where T : class
    {
        propertyBuilder.HasConversion(
            v => System.Text.Json.JsonSerializer.Serialize(v, JsonOptions.Default),
            v => System.Text.Json.JsonSerializer.Deserialize<List<T>>(v, JsonOptions.Default) ?? new List<T>()
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<List<T>>(
                (a, b) => JsonSequenceEqual(a, b),
                v => GetJsonHashCode(v),
                v => v == null ? new List<T>() : DeepClone(v)
            )
        );

        return propertyBuilder;
    }

    public static PropertyBuilder<List<T>?> HasJsonNullableListComparer<T>(this PropertyBuilder<List<T>?> propertyBuilder)
        where T : class
    {
        propertyBuilder.HasConversion(
            v => System.Text.Json.JsonSerializer.Serialize(v, JsonOptions.Default),
            v => System.Text.Json.JsonSerializer.Deserialize<List<T>>(v, JsonOptions.Default)
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<List<T>?>(
                (a, b) => JsonSequenceEqual(a, b),
                v => GetJsonHashCode(v),
                v => v == null ? null : DeepClone(v)
            )
        );

        return propertyBuilder;
    }

    public static PropertyBuilder<List<T>> HasJsonScalarListComparer<T>(this PropertyBuilder<List<T>> propertyBuilder)
        where T : struct
    {
        propertyBuilder.HasConversion(
            v => System.Text.Json.JsonSerializer.Serialize(v, JsonOptions.Default),
            v => System.Text.Json.JsonSerializer.Deserialize<List<T>>(v, JsonOptions.Default) ?? new List<T>()
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
            v => System.Text.Json.JsonSerializer.Serialize(v, JsonOptions.Default),
            v => System.Text.Json.JsonSerializer.Deserialize<List<T>>(v, JsonOptions.Default)
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
        System.Text.Json.JsonSerializer.Serialize(value, JsonOptions.Default);

    private static T DeepClone<T>(T value) =>
        System.Text.Json.JsonSerializer.Deserialize<T>(Serialize(value), JsonOptions.Default)!;

    private static bool JsonSequenceEqual<T>(IReadOnlyCollection<T>? a, IReadOnlyCollection<T>? b)
        where T : class
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        return Serialize(a) == Serialize(b);
    }

    private static int GetJsonHashCode<T>(IReadOnlyCollection<T>? items)
        where T : class
        => items == null ? 0 : Serialize(items).GetHashCode();

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
