using BlazorMHD.UI.Core.Services;
using ProjectManagement.Components.Pages;

namespace ProjectManagement.Services
{
    public class AppErrorDialog
    {
        private readonly DialogService _dialog;

        public AppErrorDialog(DialogService dialog)
        {
            _dialog = dialog;
        }

        public void Show(string title, string message, string? traceId = null)
        {
            var parameters = new Dictionary<string, object?>
            {
                [nameof(ErrorDialogUI.Title)] = title,
                [nameof(ErrorDialogUI.Message)] = message,
                [nameof(ErrorDialogUI.TraceId)] = traceId,
            };

            // نفس أسلوبك تمامًا
            _dialog.ShowComponent<ErrorDialogUI>(
                title,
                parameters!,
                DialogSize.Medium
            );
        }
    }

}
