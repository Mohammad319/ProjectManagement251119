using Microsoft.Extensions.Localization;
using ProjectManagement.Adminstrator.Shared.ResourceFiles;
using ProjectManagement.Adminstrator.Services.MHDBlazor;

namespace ProjectManagement.Adminstrator.Handless
{
    public class Logger(IStringLocalizer<ResourceLoc> Localizer, MhdServices Mhd) : ILoggerPM
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
        /// عرض رسالة باستخدام خدمة الترجمة.
        /// </summary>
        private void ShowMessage(string title, string content)
        {
            Mhd.MessageOk(Localizer[title], Localizer[content]);
        }
    }

}
