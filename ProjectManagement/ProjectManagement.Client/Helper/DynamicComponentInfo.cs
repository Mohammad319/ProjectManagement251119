using System;
using System.Collections.Generic;

namespace ProjectManagement.Client.Helper
{
    /// <summary>
    /// نموذج لتحديد مكون Blazor ديناميكي مع معلماته.
    /// </summary>
    public class DynamicComponentInfo
    {
        /// <summary>
        /// نوع المكون (مثل: typeof(MyComponent)).
        /// </summary>
        public Type ComponentType { get; set; } = typeof(object);

        /// <summary>
        /// معلمات المكون، مثل القيم أو الإعدادات.
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}
