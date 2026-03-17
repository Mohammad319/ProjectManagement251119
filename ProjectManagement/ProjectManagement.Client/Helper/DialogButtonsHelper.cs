using BlazorMHD.UI.Core.DesignSystem;
using BlazorMHD.UI.Core.Services;
using ProjectManagement.Client.Shared.ResourceFiles;

namespace ProjectManagement.Client.Helper;

public static class DialogButtonsHelper
{
    public static List<DialogButtonModel> CreateSaveCancelButtons(string formId)
    {
        return
        [
            new DialogButtonModel
            {
                Text = ResourceApp.save,
                State = MhdState.Success,
                IsPrimary = true,
                Type = "submit",
                FormId = formId
            },
            new DialogButtonModel
            {
                Text = ResourceApp.cancel,
                State = MhdState.Secondary
            }
        ];
    }
}
