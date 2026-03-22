using ProjectManagement.Client.Shared.ResourceFiles.APP;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace ProjectManagement.Client.Extensions
{
    public static class JsonHelper
    {
        private static string Localized(string key, string fallback)
            => ResourceApp.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;

        /// <summary>
        /// Convert JSON to an object of type T using web defaults.
        /// </summary>
        public static T FromJsonWeb<T>(this object obj)
        {
            var json = obj?.ToString();
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(obj), Localized("inputValueRequired", "The input value cannot be null or empty."));

            var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (result is null)
                throw new InvalidOperationException(Localized("jsonDeserializationFailed", "Failed to convert the JSON value to the requested object."));

            return result;
        }
    }
}
