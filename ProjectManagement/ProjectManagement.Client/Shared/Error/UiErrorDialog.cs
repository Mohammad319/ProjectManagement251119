using BlazorMHD.UI.Core.Services;
using ProjectManagement.Client.Shared.Repositories;

namespace ProjectManagement.Client.Shared.Error
{
    public class UiErrorDialog(DialogService dialog): IErrorDialog
    {
        public void Show(string title, string message, string? traceId = null)
        {
            dialog.ShowComponent<ApiErrorDialogUI>(
                title,Icons.Folder,
                new Dictionary<string, object?>
                {
                    [nameof(ApiErrorDialogUI.Title)] = title,
                    [nameof(ApiErrorDialogUI.Message)] = message,
                    [nameof(ApiErrorDialogUI.TraceId)] = traceId
                },
                DialogSize.Medium
            );
        }
    }

}
