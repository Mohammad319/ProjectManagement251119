using Application.Feature.Calculation.TaskStatus.Commands;
using Application.Feature.Calculation.TaskStatus.Queries;
using DocumentFormat.OpenXml.Office2010.Excel;
using Domain.Entities.Calculation;
using Microsoft.AspNetCore.Components;

using ProjectManagement.Client.Shared.Model.Project;
using ProjectManagement.Client.Shared.ResourceFiles;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Components.ControlComponents.TaskStatus
{
    public partial class IndexUI
    {

        bool IsVisible = true;
        List<TaskStatusEntity>? Status;

        void UpdateForm(TaskStatusEntity model) =>
            MHD.Modal.ShowComponent<TaskFormUI>(model.Id == 0 ? AppLoc[LocalizerConst.New, ResourceLoc.taskStatus] :
                AppLoc[LocalizerConst.Update, model.Name],
        new Dictionary<string, object> { [nameof(TaskFormUI.Status)] = model, [nameof(TaskFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, BtnUpdate) });

        void Remove(TaskStatusEntity status)
        {
            MHD.DeleteMessage(status.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(status)));
        }

        async Task ConfirmRemoveAsync(TaskStatusEntity st)
        {
            bool result = await MicroBus.Send(new DeleteTaskStatusCommand(st.Id));
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
                Status = await MicroBus.Send(new GetTaskStatusQuery());
                StateHasChanged();
            }
        }

        protected async override Task OnInitializedAsync()
        {
            Status = await MicroBus.Send(new GetTaskStatusQuery());
        }
    }
}
