using System;
using System.Linq;
using System.Text.Json;

namespace ProjectManagement.Client.Extensions
{
    public static class JsonHelper
    {
        /// <summary>
        /// تحويل JSON إلى كائن من نوع TTask باستخدام إعدادات Web.
        /// </summary>
        public static T FromJsonWeb<T>(this object obj)
        {
            string? json = obj?.ToString();
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(obj), "القيمة المدخلة لا يمكن أن تكون null أو فارغة.");

            var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return result ?? throw new JsonException("Failed to deserialize json to the requested type.");
        }
     }
}
