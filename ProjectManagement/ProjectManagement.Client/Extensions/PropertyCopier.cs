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
            if (obj is null || string.IsNullOrWhiteSpace(obj.ToString()))
                throw new ArgumentNullException(nameof(obj), "القيمة المدخلة لا يمكن أن تكون null أو فارغة.");

            return JsonSerializer.Deserialize<T>(obj.ToString(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
     }
}
