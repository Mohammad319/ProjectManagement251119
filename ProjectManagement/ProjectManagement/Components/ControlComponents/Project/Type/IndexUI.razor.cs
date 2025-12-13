using Application.Feature.Project.Type.Commands;
using Application.Feature.Project.Type.Queries;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Type
{
    public partial class IndexUI
    {
        bool IsVisible = true;
        List<TypeEntity>? Status;
        void UpdateForm(TypeEntity model) =>
    MHD.Modal.ShowComponent<TypeFormUI>(model.Id == 0 ? AppLoc[LocalizerConst.New, CalcResource.type] :
                AppLoc[LocalizerConst.Update, model.Name],
new Dictionary<string, object> { [nameof(TypeFormUI.Status)] = model, [nameof(TypeFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdate) });

        void Remove(TypeEntity status) =>
            MHD.DeleteMessage(status.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(status)));
        async Task ConfirmRemoveAsync(TypeEntity st)
        {
            bool result = await MicroBus.Send(new DeleteTypeCommand(st.Id));
            if (result)
            {
                Status?.Remove(st);
                StateHasChanged();
            }
            MHD.Notifications(ToastType.Delete, result);
        }
        async Task BtnUpdate(bool IsSuccess)
        {
            MHD.Modal.Close();
            if (IsSuccess)
            {
                Status = await MicroBus.Send(new GetTypeQuery());
                StateHasChanged();
            }
        }

        protected async override Task OnInitializedAsync()
        {
            Status = await MicroBus.Send(new GetTypeQuery());
        }
    }
}
