using Application.Feature.Project.Type.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Client.Shared.Model.Project;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Project;
using System.Threading.Tasks;

namespace ProjectManagement.Components.ControlComponents.Project.Type
{
    public partial class TypeFormUI
    {
        [Parameter] public TypeEntity Status { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }

        bool IsLoading = false;
        private PostTaskStatusDTO UpdateObj = new();

        protected override void OnInitialized()
        {
            UpdateObj.Name = Status.Name;
            UpdateObj.Color = Status.Color; 
            UpdateObj.Order = Status.SortOrder;       
        }
        private async Task HandleSubmitAsync()
        {
            IsLoading = true;
            bool result = false;
            if (Status.Id == 0)
                result = await MicroBus.Send(new CreateTypeCommand(UpdateObj)) > 0;
            else result = await MicroBus.Send(new UpdateTypeCommand(Status.Id,UpdateObj));

            MHD.Notifications(Status.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
    }
}
