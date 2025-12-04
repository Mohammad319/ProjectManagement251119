using System;
using System.Linq;
using System.Text.Json;

namespace ProjectManagement.Client.Extensions
{
    public static class PropertyCopier
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

        /// <summary>
        /// نسخ الخصائص المتطابقة من كائن المصدر إلى الهدف.
        /// </summary>
        public static TU CopyPropertiesTo<T, TU>(this T source, TU dest)
        {
            var sourceProps = typeof(T).GetProperties().Where(p => p.CanRead);
            var destProps = typeof(TU).GetProperties().Where(p => p.CanWrite).ToDictionary(p => p.Name);

            foreach (var prop in sourceProps)
            {
                if (destProps.TryGetValue(prop.Name, out var targetProp) && targetProp.PropertyType == prop.PropertyType)
                {
                    targetProp.SetValue(dest, prop.GetValue(source));
                }
            }

            return dest;
        }
    }
}
