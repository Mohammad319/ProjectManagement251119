using BlazorMHD.UI.Core.DesignSystem;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace ProjectManagement.Client.Services.MHDBlazor
{
    public enum ToastType
    {
        Delete,
        Add,
        Update,
        Info,
        Danger
    }

    public class MhdServices(DialogService modal, LoadingService loading,
        ToastService ToasterService
        , IStringLocalizer<ResourceApp> appLocalizer)
    {
        private readonly IStringLocalizer<ResourceApp> _appLocalizer = appLocalizer;
        public DialogService Modal => modal;
        public LoadingService Loading => loading;

        #region حوارات التأكيد / الرسائل

        /// <summary>
        /// رسالة تأكيد الحذف (Yes/No)
        /// </summary>
        /// 
        public void DeleteMessage(string? itemName, EventCallback? onYes = null)
        {
            var title = ResourceApp.delete;
            var message = string.IsNullOrEmpty(itemName)
                ? _appLocalizer[ResourceApp.deleteConfirmMsg]
                : _appLocalizer[LocalizerConst.deleteConfirmMsg, itemName];

            var model = new MhdDialogModel
            {
                Title = _appLocalizer[title],
                Content = builder =>
                {
                    builder.OpenElement(0, "p");
                    builder.AddAttribute(1, "class", "text-sm leading-relaxed text-slate-700 dark:text-slate-300");
                    builder.AddContent(2, message);
                    builder.CloseElement();
                },
                CloseOnOverlayClick = false,
                Buttons =
                {
                    new MhdDialogButtonModel
                    {
                        Text = _appLocalizer[ResourceApp.yes],
                        State = MhdState.Danger,
                        IsPrimary = true,
                        OnClick = onYes ?? EventCallback.Factory.Create(this, () => Modal.CloseAsync())
                    },
                    new MhdDialogButtonModel
                    {
                        Text = _appLocalizer[ResourceApp.no],
                        State = MhdState.Secondary,
                        OnClick = EventCallback.Factory.Create(this, () => Modal.CloseAsync())
                    }
                },
                Size = MhdDialogSize.Medium,
                State = MhdState.Danger,
            };

            Modal.Show(model);
        }

        /// <summary>
        /// رسالة Yes/No عامة
        /// </summary>
        public void MessageYesNo(
            string title,
            string message,
            MhdState state = MhdState.Danger,
            EventCallback? onYes = null)
        {
            MhdDialogModel model = new()
            {
                Size = MhdDialogSize.Medium,
                State = state,
                Title = title,
                Content = builder =>
                {
                    builder.OpenElement(0, "p");
                    builder.AddAttribute(1, "class", "text-sm text-slate-500 dark:text-slate-400");
                    builder.AddContent(2, message);
                    builder.CloseElement();
                },
                CloseOnOverlayClick = false,
                Buttons =
                {
                    new MhdDialogButtonModel
                    {
                        Text = _appLocalizer[ResourceApp.yes],
                        State = state,
                        IsPrimary = true,
                        OnClick = onYes ?? EventCallback.Factory.Create(this, () => Modal.CloseAsync())
                    },
                    new MhdDialogButtonModel
                    {
                        Text = _appLocalizer[ResourceApp.no],
                        State = MhdState.Neutral,
                        IsPrimary = false,
                        OnClick = EventCallback.Factory.Create(this, () => Modal.CloseAsync())
                    }
                },

            };

            modal.Show(model);
        }

        /// <summary>
        /// رسالة OK فقط
        /// </summary>
        public void MessageOk(
            string title,
            string message,
            MhdState state = MhdState.Danger,
            EventCallback? onOk = null)
        {
            // لو مافي كولباك، نستخدم ShowSimple
            if (onOk == null || !onOk.Value.HasDelegate)
            {
                Modal.ShowSimple(title, message, _appLocalizer["ok"]);
                return;
            }

            var model = new MhdDialogModel
            {
                Title = title,
                State = state,
                Content = builder =>
                {
                    builder.OpenElement(0, "p");
                    builder.AddAttribute(1, "class", "text-sm text-slate-500 dark:text-slate-400");
                    builder.AddContent(2, message);
                    builder.CloseElement();
                },
                CloseOnOverlayClick = true,
                Buttons =
                {
                    new MhdDialogButtonModel
                    {
                        Text = _appLocalizer["ok"],
                        State = state,
                        IsPrimary = true,
                        OnClick = onOk
                    }
                }
            };

            Modal.Show(model);
        }

        #endregion

        #region Notifications القديمة (بالتوست)

        public void Notifications(ToastType type = ToastType.Delete, bool isSuccess = false, string name = "")
        {
            if (isSuccess)
            {

                if (type == ToastType.Delete)
                    ToasterService.Show(name, ResourceApp.HasBeenRemoved, MhdState.Danger);
                if (type == ToastType.Add)
                    ToasterService.Show(name, ResourceApp.HasBeenAdded, MhdState.Success);
                if (type == ToastType.Update)
                    ToasterService.Show(name, ResourceApp.HasBeedUpdated, MhdState.Success);
                if (type == ToastType.Info)
                    ToasterService.Show(name, ResourceApp.completedSuccessfully, MhdState.Success);
            }
            else
            {
                if (type == ToastType.Delete)
                {
                    MessageOk(
                        _appLocalizer[ResourceApp.error],
                        _appLocalizer[ResourceApp.ItemIsCurrentlyInUseCannotBeDeleted_],
                        MhdState.Danger);
                }
                else if (type is ToastType.Update or ToastType.Add or ToastType.Info)
                {
                    MessageOk(
                        _appLocalizer[ResourceApp.error],
                        _appLocalizer[ResourceApp.AnUnexpectedErrorHasOccurred],
                        MhdState.Danger);
                }
            }
        }

        #endregion

        #region ToastMessage الجديدة/القديمة

        public void ToastMessage(string? itemName, ToastType type = ToastType.Add, bool isSuccess = true)
        {
            ToastMessage(itemName, type, isSuccess ? MhdState.Success : MhdState.Danger);
        }

        public void ToastMessage(string? itemName, ToastType type, MhdState state)
        {

            if (string.IsNullOrEmpty(itemName))
            {
                //ToasterService.AddToast(
                //    Toast.New("",
                //        _appLocalizer[ResourceApp.completedSuccessfully],
                //        state,
                //        time));

                return;
            }

            if (type == ToastType.Delete)
            {
                ToasterService.Show(itemName, _appLocalizer[LocalizerConst.hasBeenDeletedSuccessfully, itemName], state);

            }
            else if (type == ToastType.Add)
            {
                ToasterService.Show(itemName, _appLocalizer[LocalizerConst.hasBeenAddedSuccessfully, itemName], state);

            }
            else if (type == ToastType.Update)
            {
                ToasterService.Show(itemName, _appLocalizer[LocalizerConst.hasBeenUpdatedSuccessfully, itemName], state);

            }
            else if (type == ToastType.Info)
            {
                ToasterService.Show(itemName, _appLocalizer[ResourceApp.completedSuccessfully], state);

            }
        }

        #endregion
    }
}
