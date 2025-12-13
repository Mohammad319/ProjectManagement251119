using Application.Feature.Calculation.TaskStatus.Commands;
using Domain.Entities.Calculation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Components.ControlComponents.TaskStatus
{
    public partial class TaskFormUI
    {
        [Parameter] public TaskStatusEntity Status { get; set; }
        PostTaskStatusDTO PostStatus { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }
        bool IsLoading = false;
        protected override void OnInitialized()
        {
            PropertyCopier.CopyPropertiesTo(Status, PostStatus);
        }
        private async Task HandleSubmitAsync()
        {
            try
            {
                IsLoading = true;
                bool result;
                if (Status.Id == 0)
                    result = await MicroBus.Send(new CreateTaskStatusCommand(PostStatus)) > 0;
                else result = await MicroBus.Send(new UpdateTaskStatusCommand(Status.Id, PostStatus));

                MHD.Notifications(Status.Id == 0 ? ToastType.Add : ToastType.Update, result);
                await Callback.InvokeAsync(result);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:" + ex);
            }
        }
    }
}
