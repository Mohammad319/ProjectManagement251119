using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Client.Shared.Components;

internal sealed class RequiredFieldCssClassProvider : FieldCssClassProvider
{
    public static RequiredFieldCssClassProvider Instance { get; } = new();

    private static readonly ConcurrentDictionary<(Type ModelType, string FieldName), FieldMetadata> MetadataCache = new();

    public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        var hasMessages = editContext.GetValidationMessages(fieldIdentifier).Any();
        var isModified = editContext.IsModified(fieldIdentifier);
        var isRequiredEmpty = IsRequiredEmpty(fieldIdentifier);

        var classes = new List<string>(3);

        if (isModified)
            classes.Add("modified");

        if (hasMessages)
            classes.Add("invalid");
        else if (isModified)
            classes.Add("valid");

        if (isRequiredEmpty)
            classes.Add("required-empty");

        return string.Join(" ", classes);
    }

    private static bool IsRequiredEmpty(FieldIdentifier fieldIdentifier)
    {
        var metadata = GetMetadata(fieldIdentifier);
        if (!metadata.IsRequired || metadata.Property is null)
            return false;

        var value = metadata.Property.GetValue(fieldIdentifier.Model);
        return IsEmpty(value);
    }

    private static FieldMetadata GetMetadata(FieldIdentifier fieldIdentifier)
    {
        var modelType = fieldIdentifier.Model.GetType();
        return MetadataCache.GetOrAdd(
            (modelType, fieldIdentifier.FieldName),
            key =>
            {
                var property = key.ModelType.GetProperty(
                    key.FieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                return new FieldMetadata(property, IsRequiredProperty(property, key.ModelType, key.FieldName));
            });
    }

    private static bool IsRequiredProperty(PropertyInfo? property, Type modelType, string fieldName)
    {
        if (property?.GetCustomAttribute<RequiredAttribute>(true) is not null)
            return true;

        if (property?.GetCustomAttributes<ValidationAttribute>(true)
                .Any(attribute => string.Equals(
                    attribute.ErrorMessageResourceName,
                    ErrorsMessages.FieldIsRequred,
                    StringComparison.Ordinal)) == true)
            return true;

        return modelType == typeof(ResourcePostDTO) && fieldName == nameof(ResourcePostDTO.Quantity)
            || modelType == typeof(TaskMetadata) && fieldName == nameof(TaskMetadata.BaseQuantity);
    }

    private static bool IsEmpty(object? value)
    {
        if (value is null)
            return true;

        if (value is string text)
            return string.IsNullOrWhiteSpace(text);

        if (value is DateTime dateTime)
            return dateTime == default;

        return false;
    }

    private sealed record FieldMetadata(PropertyInfo? Property, bool IsRequired);
}
