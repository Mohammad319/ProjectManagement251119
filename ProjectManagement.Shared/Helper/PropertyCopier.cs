using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectManagement.Shared.Helper
{
    public static class PropertyCopier
    {
        public static TU CopyPropertiesTo<T, TU>(this T source, TU dest)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (dest is null) throw new ArgumentNullException(nameof(dest));

            var sourceProps = typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .ToArray();

            var destProps = typeof(TU).GetProperties()
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name);

            foreach (var sp in sourceProps)
            {
                if (!destProps.TryGetValue(sp.Name, out var dp))
                    continue;

                if (dp.PropertyType != sp.PropertyType)
                    continue;

                dp.SetValue(dest, sp.GetValue(source));
            }

            return dest;
        }
    }

}
