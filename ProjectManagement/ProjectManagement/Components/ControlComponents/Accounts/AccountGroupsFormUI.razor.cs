using Application.Feature.Account.Commands;
using BlazorMHD.UI.Core.Services;
using Domain.Entities.Calculation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Account;

namespace ProjectManagement.Components.ControlComponents.Accounts
{
    public partial class AccountGroupsFormUI
    {
        [Parameter] public required AccountGroupEntity AccountGroup { get; set; }
        [Parameter] public EventCallback<bool> Callback { get; set; }
        [Inject] DialogService DialogService { get; set; }

        PostAccountGroupDTO PostDTO { get; set; } = new();
        bool IsLoading = false;

        void CloseModal() => DialogService.Close();

        protected override void OnInitialized()
        {
            PropertyCopier.CopyPropertiesTo(AccountGroup, PostDTO);
        }

        public async Task<bool> NewUpdateAsync(int id, PostAccountGroupDTO PostDTO)
        {
            bool result;
            result = id == 0 ? await MicroBus.Send(new CreateAccountGroupCommand(PostDTO)) > 0 :
                await MicroBus.Send(new UpdateAccountGroupCommand(PostDTO, id));
            MHD.Notifications(id > 0 ? ToastType.Update : ToastType.Add, result);
            return result;
        }
        private async Task HandleSubmitAsync()
        {
            IsLoading = true;
            try
            {
                await Callback.InvokeAsync(await NewUpdateAsync(AccountGroup.Id, PostDTO));
            }
            finally
            {
                IsLoading = false;
                CloseModal();
            }
        }
    }
}
