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
            var json = obj?.ToString();
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(obj), "القيمة المدخلة لا يمكن أن تكون null أو فارغة.");

            var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (result is null)
                throw new InvalidOperationException("تعذر تحويل JSON إلى الكائن المطلوب.");

            return result;
        }
     }
}
