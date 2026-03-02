using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Helper
{
    public sealed class OptionalUrlAttribute : UrlAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is string text && string.IsNullOrWhiteSpace(text))
                return true;

            return base.IsValid(value);
        }
    }
}
