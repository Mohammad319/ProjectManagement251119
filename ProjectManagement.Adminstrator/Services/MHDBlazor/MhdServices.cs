using BlazorMHD.Component.Toasts;
using BlazorMHD.Model;
using Microsoft.Extensions.Localization;
using ProjectManagement.Adminstrator.Constants;

namespace ProjectManagement.Client.Adminstrator.Services.MHDBlazor
{
    public enum ToastType
    {
        Delete, Add, update, Info, Danger
    }
    public class MhdServices(MHDService Service, IStringLocalizer<ResourceApp> appLocalizer)
    {
        public DialogService Modal => Service.DialogService;
        public LoadingService Loading => Service.LoadingService;
        public MhdMessageBoxService Message => Service.Message;
        public void DeleteMessage(string itemName, Func<object> func)
        {
            MhdMessageBoxModel msg = new()
            {
                Title = ResourceApp.delete,
                Message = string.IsNullOrEmpty(itemName) ?
            appLocalizer[ResourceApp.deleteConfirmMsg] : appLocalizer[LocalizerConst.deleteConfirmMsg, itemName],
                State = MhdState.Danger,
                Buttons =
           [
               new (){ Color = MhdState.Danger, Text =ResourceApp.yes ,Func = func},
                   new() { Color = MhdState.Primary, Text = ResourceApp.no, Func = null }
           ]
            };

            Service.Message.AddMessage(msg);
        }

        public void MessageYesNo(string title, string message, MhdState state =
            MhdState.Danger, Func<object> func = null)
        {
            MhdMessageBoxModel msg = new()
            {
                Title = title,
                Message = message,
                State = MhdState.Danger,
                Buttons =
               [
                   new (){ Color = MhdState.Primary, Text =ResourceApp.yes ,Func = func},
                   new() { Color = MhdState.Primary, Text = ResourceApp.no, Func = null }
               ]
            };
            Service.Message.AddMessage(msg);
        }
        public void Notifications(ToastType type = ToastType.Delete, bool isSuccess = false, string name = "")
        {
            if (isSuccess)
            {
                int time = 3;
                if (type == ToastType.Delete)
                    Service.ToasterService.AddToast(Toast.New(name, ResourceApp.HasBeenRemoved, MhdState.Success, time));
                else if (type == ToastType.Add)
                    Service.ToasterService.AddToast(Toast.New(name, ResourceApp.HasBeenAdded, MhdState.Success, time));
                else if (type == ToastType.update)
                    Service.ToasterService.AddToast(Toast.New(name, ResourceApp.HasBeedUpdated, MhdState.Success, time));
                else if (type == ToastType.Info)
                    Service.ToasterService.AddToast(Toast.New(name, ResourceApp.completedSuccessfully, MhdState.Success, time));

            }
            else
            {
                if (type == ToastType.Delete)
                    MessageOk(ResourceApp.error, ResourceApp.ItemIsCurrentlyInUseCannotBeDeleted_, MhdState.Danger);
                else if (type == ToastType.update || type == ToastType.Add || type == ToastType.Info)
                    MessageOk(ResourceApp.error, ResourceApp.AnUnexpectedErrorHasOccurred, MhdState.Danger);
            }
        }

        public void MessageOk(string title, string message, MhdState state = MhdState.Danger,
            Func<object> func = null)
        {
            MhdMessageBoxModel msg = new()
            {
                Title = title,
                Message = message,
                State = state,
                Buttons =
               [
                   new (){ Color = MhdState.Primary, Text =appLocalizer["ok"] ,Func = func},
               ]
            };
            Service.Message.AddMessage(msg);
        }
        public void ToastMessage(string itemName, ToastType type = ToastType.Add, bool IsSuccess = true)
        {
            ToastMessage(itemName, type, IsSuccess ? MhdState.Success : MhdState.Danger);
        }
        public void ToastMessage(string itemName, ToastType type, MhdState state)
        {
            int time = 20;
            string msg = appLocalizer[ResourceApp.completedSuccessfully];
            if (string.IsNullOrEmpty(itemName))
                Service.ToasterService.AddToast(Toast.New("", appLocalizer[ResourceApp.completedSuccessfully], state, time));

            else if (type == ToastType.Delete)
                Service.ToasterService.AddToast(Toast.New(itemName, appLocalizer[LocalizerConst.hasBeenDeletedSuccessfully, itemName], state, time));
            else if (type == ToastType.Add)
                Service.ToasterService.AddToast(Toast.New(itemName, appLocalizer[LocalizerConst.hasBeenAddedSuccessfully, itemName], state, time));
            else if (type == ToastType.update)
                Service.ToasterService.AddToast(Toast.New(itemName, appLocalizer[LocalizerConst.hasBeenUpdatedSuccessfully, itemName], state, time));
            else if (type == ToastType.Info)
                Service.ToasterService.AddToast(Toast.New(itemName,
                    appLocalizer[LocalizerConst.hasBeenDeletedSuccessfully, itemName], state, time));
        }
    }
}
