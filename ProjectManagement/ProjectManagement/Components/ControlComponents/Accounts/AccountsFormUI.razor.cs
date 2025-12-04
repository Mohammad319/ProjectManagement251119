using Application.Feature.Account.Commands;
using Application.Feature.Account.Queries.AccountGroup;
using BlazorMHD.UI.Core.Services;
using DocumentFormat.OpenXml.Office.Word;
using Domain.Entities.Calculation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Account;

namespace ProjectManagement.Components.ControlComponents.Accounts
{
    public partial class AccountsFormUI
    {
        [Parameter] public AccountEntity Account { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }
        [Inject] DialogService DialogService { get; set; }

        PostAccountDTO PostAccountDTO { get; set; } = new PostAccountDTO();
        bool IsLoading = false;
        List<ListAccountGroupIncludeAccountDTO>? GroupsAPI;
        void CloseModal() => DialogService.Close();

        protected async override Task OnInitializedAsync()
        {
            PropertyCopier.CopyPropertiesTo(Account, PostAccountDTO);
            GroupsAPI = await MicroBus.Send(new GetAccountGroupsAsListQuery());
        }
        private async Task HandleSubmitAsync()
        {
            IsLoading = true;
           await Callback.InvokeAsync(await NewUpdateAsync(Account.Id, PostAccountDTO));
            CloseModal();
            IsLoading = false;
        }
        public async Task<bool> NewUpdateAsync(int id, PostAccountDTO PostDTO)
        {
            bool result;
            result = id == 0 ? await MicroBus.Send(new CreateAccountCommand(PostDTO)) > 0 :
                await MicroBus.Send(new UpdateAccountCommand(PostDTO, id));
            MHD.Notifications(id > 0 ? ToastType.Update : ToastType.Add, result);
            return result;
        }
    }
}
