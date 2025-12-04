using System;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Handless
{
    public interface IExceptionHandlers
    {
        /// <summary>
        /// ينفذ عملية ويقوم بالتعامل مع الاستثناءات إن وجدت.
        /// </summary>
        T Run<T>(Func<T> operation);

        /// <summary>
        /// ينفذ عملية غير متزامنة مع التحقق من التوكن والتعامل مع الأخطاء.
        /// </summary>
        Task<T> RunCheckTokenAsync<T>(Func<Task<T>> operation);
    }
}
