using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components.Authorization;
using ProjectManagement.Client.Shared.Exception;
using System.Security.Claims;

namespace ProjectManagement.Client.Handless
{
    public class ExceptionHandlers(
        ILogger _loger,
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
                return default;
            }
        }

        /// <summary>
        /// الحصول على Claim انتهاء صلاحية التوكن (exp) من المستخدم.
        /// </summary>
        private async Task<Claim> GetExpirationClaimAsync()
        {
            var user = (await authStateProvider.GetAuthenticationStateAsync()).User;
            return user?.FindFirst(u => u.Type.Equals("exp"));
        }

        /// <summary>
        /// تنفيذ آمن لعملية غير متزامنة، مع التعامل مع انتهاء التوكن وتسجيل الأخطاء.
        /// </summary>
        public async Task<T> RunCheckTokenAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                var result = await operation.Invoke();
                return result;
            }
            catch (HttpResponseException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _loger.Log(ex);
            }
            catch (HttpRequestException ex)
            {
                _loger.Log(ex);
            }
            catch (Exception e)
            {
                _loger.Log(e);
            }
            finally
            {
            }

            return default;
        }
    }
}
