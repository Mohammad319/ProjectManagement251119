using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Shared.Exception;
using ProjectManagement.Client.Shared.ResourceFiles;
using System;
using System.Net.Http;

namespace ProjectManagement.Client.Handless
{
    public class Logger(IStringLocalizer<ResourceLoc> Localizer, MhdServices Mhd) : ILogger
    {
        /// <summary>
        /// تسجيل استثناء عام.
        /// </summary>
        public void Log(Exception ex)
        {
            ShowMessage("Error", ex.Message);
        }

        /// <summary>
        /// تسجيل أنواع مختلفة من أخطاء HttpRequestException.
        /// </summary>
        public void Log(HttpRequestException ex)
        {
            var message = ex.HttpRequestError switch
            {
                HttpRequestError.ResponseEnded => "ResponseEnded",
                HttpRequestError.InvalidResponse => "InvalidResponse",
                HttpRequestError.HttpProtocolError => "HttpProtocolError",
                HttpRequestError.ProxyTunnelError => "ProxyTunnelError",
                HttpRequestError.NameResolutionError => "NameResolutionError",
                HttpRequestError.ConnectionError => "ConnectionError",
                HttpRequestError.ExtendedConnectNotSupported => "ExtendedConnectNotSupported",
                HttpRequestError.SecureConnectionError => "SecureConnectionError",
                HttpRequestError.UserAuthenticationError => "UserAuthenticationError",
                HttpRequestError.Unknown => "Unknown",
                _ => "UnknownError"
            };

            ShowMessage("Http Error", message);
        }

        /// <summary>
        /// تسجيل استثناء مرتبط برد HTTP.
        /// </summary>
        public void Log(HttpResponseException ex)
        {
            var title = ex.HttpStatusCode.ToString();
            var content = ex.HttpStatusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => "BadRequest: " + ex.Text,
                _ => ex.Message ?? ex.Text
            };

            ShowMessage(title, content);
        }

        /// <summary>
        /// عرض رسالة باستخدام خدمة الترجمة.
        /// </summary>
        private void ShowMessage(string title, string content)
        {
            Mhd.MessageOk(Localizer[title], Localizer[content]);
        }
    }

}
