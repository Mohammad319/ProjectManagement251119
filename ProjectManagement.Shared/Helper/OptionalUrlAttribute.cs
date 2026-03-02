using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Helper
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class OptionalUrlAttribute : ValidationAttribute
    {
        private static readonly UrlAttribute UrlValidator = new();

        public override bool IsValid(object? value)
        {
            if (value is null)
                return true;

            if (value is not string text)
                return false;

            if (string.IsNullOrWhiteSpace(text))
                return true;

            return UrlValidator.IsValid(text);
        }
    }
}
