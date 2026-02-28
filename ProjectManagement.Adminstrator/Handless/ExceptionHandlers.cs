using Microsoft.AspNetCore.Components.Authorization;
using ProjectManagement.Adminstrator.Handless;
using System.Security.Claims;

namespace ProjectManagement.Client.Adminstrator.Handless
{
    public class ExceptionHandlers(
        ILoggerPM _loger,
        AuthenticationStateProvider authStateProvider,
        LoadingService loadingService)
        : IExceptionHandlers, IDisposable
    {
        public void Dispose() { }

        /// <summary>
        /// تنفيذ آمن لعملية متزامنة مع تسجيل الأخطاء.
        /// </summary>
        public T Run<T>(Func<T> operation)
        {
            try
            {
                return operation.Invoke();
            }
            catch (Exception e)
            {
                _loger.Log(e);
                return default!;
            }
        }

        /// <summary>
        /// الحصول على Claim انتهاء صلاحية التوكن (exp) من المستخدم.
        /// </summary>
        private async Task<Claim?> GetExpirationClaimAsync()
        {
            var user = (await authStateProvider.GetAuthenticationStateAsync()).User;
            return user.FindFirst(u => u.Type.Equals("exp"));
        }

        /// <summary>
        /// تنفيذ آمن لعملية غير متزامنة، مع التعامل مع انتهاء التوكن وتسجيل الأخطاء.
        /// </summary>
        public async Task<T> RunCheckTokenAsync<T>(Func<Task<T>> operation)
        {
            loadingService.Begin();
            try
            {
                var result = await operation.Invoke();
                return result;
            }
            catch (Exception e)
            {
                _loger.Log(e);
            }
            finally
            {
                loadingService.End();
            }

            return default!;
        }
    }
}
